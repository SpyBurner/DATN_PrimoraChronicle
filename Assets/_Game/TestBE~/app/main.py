import os
import json
from fastapi import FastAPI, Depends, HTTPException
from fastapi.responses import RedirectResponse
from fastapi.staticfiles import StaticFiles
from sqlalchemy.orm import Session
from typing import List
from datetime import datetime

from . import models, schemas
from .database import engine, get_db

models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="Primora Chronicle Test API", description="Test BE for Unity Client", version="1.0.0")

# Ensure logs dir exists
os.makedirs("app/logs", exist_ok=True)
app.mount("/static", StaticFiles(directory="app/logs"), name="static")

@app.get("/", include_in_schema=False)
def read_root():
    return RedirectResponse(url="/docs")

@app.post("/api/auth/register", response_model=dict, tags=["Auth"])
def register(user: schemas.UserCreate, db: Session = Depends(get_db)):
    db_user = db.query(models.User).filter(models.User.username == user.username).first()
    if db_user:
        raise HTTPException(status_code=400, detail="Username already registered")
    
    new_user = models.User(username=user.username, passwordHash=user.password, xpTotal=0, gold=500)
    db.add(new_user)
    db.commit()
    db.refresh(new_user)
    return {"token": f"mock_jwt_{new_user.ID}", "user": {"ID": new_user.ID, "username": new_user.username, "gold": new_user.gold}}

@app.post("/api/auth/login", response_model=dict, tags=["Auth"])
def login(creds: schemas.LoginRequest, db: Session = Depends(get_db)):
    db_user = db.query(models.User).filter(models.User.username == creds.username, models.User.passwordHash == creds.password).first()
    if not db_user:
        raise HTTPException(status_code=401, detail="Invalid credentials")
    return {"token": f"mock_jwt_{db_user.ID}", "user": {"ID": db_user.ID, "username": db_user.username, "gold": db_user.gold, "xpTotal": db_user.xpTotal}}

@app.get("/api/users/me", response_model=schemas.UserResponse, tags=["Users"])
def get_me(user_id: str, db: Session = Depends(get_db)):
    user = db.query(models.User).filter(models.User.ID == user_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    return user

@app.get("/api/collection/card-copies", response_model=List[schemas.CardCopyResponse], tags=["Collections"])
def get_card_copies(user_id: str, db: Session = Depends(get_db)):
    copies = db.query(models.CardCopy).filter(models.CardCopy.userID == user_id).all()
    return copies

@app.get("/api/decks", response_model=List[schemas.DeckResponse], tags=["Decks"])
def get_decks(user_id: str, db: Session = Depends(get_db)):
    decks = db.query(models.Deck).filter(models.Deck.userID == user_id).all()
    result = []
    for d in decks:
        card_ids = [c.ID for c in d.card_copies]
        result.append(schemas.DeckResponse(ID=d.ID, name=d.name, description=d.description, cardCopyIDs=card_ids))
    return result

@app.post("/api/decks", response_model=schemas.DeckResponse, tags=["Decks"])
def create_deck(user_id: str, deck_data: schemas.DeckCreate, db: Session = Depends(get_db)):
    deck = models.Deck(name=deck_data.name, description=deck_data.description, userID=user_id)
    db.add(deck)
    db.commit()
    db.refresh(deck)
    
    # Add cards
    copies = db.query(models.CardCopy).filter(models.CardCopy.ID.in_(deck_data.cardCopyIDs), models.CardCopy.userID == user_id).all()
    deck.card_copies.extend(copies)
    db.commit()
    
    card_ids = [c.ID for c in deck.card_copies]
    return schemas.DeckResponse(ID=deck.ID, name=deck.name, description=deck.description, cardCopyIDs=card_ids)

@app.post(
    "/api/matches/result",
    tags=["Matches"],
    summary="Submit Match Result & Upload ActionLog",
    description="""
    Submits match results, calculates XP/Gold, and creates match records.
    
    **BUCKET FILE STORAGE STRATEGY SIMULATION:**
    This Python endpoint explicitly simulates an `IStorageService` bucket upload by writing the JSON ActionLog data to a local `/logs/` folder and returning a local static URL (`FileBucketUrl`).
    
    The actual ASP.NET Backend must orchestrate a real Cloud Bucket File Storage strategy (e.g., AWS S3, Azure Blob Storage, or MinIO) to handle uploading these payload files instead of local disks.
    """
)
def submit_match_result(data: schemas.MatchResultSubmit, db: Session = Depends(get_db)):
    log = models.ActionLog()
    db.add(log)
    db.commit()
    db.refresh(log)
    
    filename = f"{log.ID}.json"
    filepath = os.path.join("app", "logs", filename)
    with open(filepath, "w") as f:
        json.dump(data.actionLogData, f)
    
    host_url = os.getenv("HOST_URL", "http://localhost:8000")
    log.fileBucketURL = f"{host_url}/static/{filename}"
    
    match = models.Match(endDateTime=datetime.utcnow(), actionLogID=log.ID)
    db.add(match)
    db.commit()
    db.refresh(match)
    
    p1 = models.MatchParticipant(isWinner=True, goldReceived=50, xpReceived=100, deckID=data.winnerDeckID, userID=data.winnerUserID, matchID=match.ID)
    p2 = models.MatchParticipant(isWinner=False, goldReceived=10, xpReceived=20, deckID=data.loserDeckID, userID=data.loserUserID, matchID=match.ID)
    
    # Update users
    u1 = db.query(models.User).filter(models.User.ID == data.winnerUserID).first()
    if u1:
        u1.gold += 50
        u1.xpTotal += 100
        
    u2 = db.query(models.User).filter(models.User.ID == data.loserUserID).first()
    if u2:
        u2.gold += 10
        u2.xpTotal += 20
    
    db.add(p1)
    db.add(p2)
    db.commit()
    
    return {"status": "success", "matchID": match.ID}

@app.get("/api/matches", response_model=List[schemas.MatchHistoryResponse], tags=["Matches"])
def get_match_history(user_id: str, db: Session = Depends(get_db)):
    participations = db.query(models.MatchParticipant).filter(models.MatchParticipant.userID == user_id).all()
    result = []
    for p in participations:
        m = p.match
        log_url = m.action_log.fileBucketURL if m.action_log else ""
        result.append(schemas.MatchHistoryResponse(
            matchID=m.ID,
            endDateTime=m.endDateTime,
            isWinner=p.isWinner,
            goldReceived=p.goldReceived,
            xpReceived=p.xpReceived,
            actionLogURL=log_url
        ))
    return result

@app.get("/api/config", tags=["System"])
def get_config(db: Session = Depends(get_db)):
    cfg = db.query(models.SystemConfig).first()
    if not cfg:
        return {}
    return {
        "dailyDealDiscountRate": cfg.dailyDealDiscountRate,
        "championCardBasePrice": cfg.championCardBasePrice,
        "commonCardBasePrice": cfg.commonCardBasePrice,
        "levelXpGrowthRate": cfg.levelXpGrowthRate,
        "startingLevelXp": cfg.startingLevelXp,
        "afkPenaltyAmount": cfg.afkPenaltyAmount
    }
