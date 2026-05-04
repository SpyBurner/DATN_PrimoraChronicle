from sqlalchemy.orm import Session
from .database import engine, SessionLocal
from . import models
import random
import json
import os
from datetime import datetime, timedelta

def seed_db():
    models.Base.metadata.create_all(bind=engine)
    db = SessionLocal()
    
    if db.query(models.User).count() > 0:
        print("Database already seeded.")
        return

    print("Seeding database with mock data...")
    
    cfg = models.SystemConfig(
        dailyDealDiscountRate=0.2,
        championCardBasePrice=1000,
        commonCardBasePrice=100,
        levelXpGrowthRate=1.5,
        startingLevelXp=1000,
        afkPenaltyAmount=50
    )
    db.add(cfg)
    
    # Based on Unity Client ChampionCardSO, SpellCardSO, TroopCardSO files
    card_ids = ["Lich", "Reject", "Duhallan"] + [f"card-{i}" for i in range(1, 48)]
    for cid in card_ids:
        db.add(models.Card(ID=cid))
        
    db.commit()

    users = []
    for i in range(1, 3):
        u = models.User(username=f"player{i}", passwordHash="securepassword", xpTotal=5000, gold=2000)
        db.add(u)
        users.append(u)
    db.commit()

    host_url = os.getenv("HOST_URL", "http://localhost:8000")
    os.makedirs("app/logs", exist_ok=True)

    for u in users:
        copies = []
        for _ in range(50):
            cc = models.CardCopy(userID=u.ID, cardID=random.choice(card_ids))
            db.add(cc)
            copies.append(cc)
        db.commit()

        decks = []
        for i in range(2):
            deck = models.Deck(name=f"Deck {i+1} for {u.username}", description="Mock deck seeded automatically", userID=u.ID)
            db.add(deck)
            decks.append(deck)
        db.commit()

        for deck in decks:
            random_copies = random.sample(copies, 20)
            deck.card_copies.extend(random_copies)
        db.commit()
    
    # 10 match histories (5 matches played between player1 and player2)
    for i in range(5):
        log = models.ActionLog()
        db.add(log)
        db.commit()
        db.refresh(log)
        
        filename = f"{log.ID}.json"
        with open(f"app/logs/{filename}", "w") as f:
            json.dump({"events": ["Game Start", f"Player 1 draws", "Player 2 draws", "Game End"]}, f)
        
        log.fileBucketURL = f"{host_url}/static/{filename}"
        
        m = models.Match(endDateTime=datetime.utcnow() - timedelta(hours=i), actionLogID=log.ID)
        db.add(m)
        db.commit()
        db.refresh(m)
        
        p1 = models.MatchParticipant(
            isWinner=(i%2==0), 
            goldReceived=50 if i%2==0 else 10,
            xpReceived=100 if i%2==0 else 20,
            deckID=users[0].decks[0].ID,
            userID=users[0].ID,
            championCardID="Lich",
            matchID=m.ID
        )
        p2 = models.MatchParticipant(
            isWinner=(i%2!=0), 
            goldReceived=50 if i%2!=0 else 10,
            xpReceived=100 if i%2!=0 else 20,
            deckID=users[1].decks[0].ID,
            userID=users[1].ID,
            championCardID="Reject",
            matchID=m.ID
        )
        db.add(p1)
        db.add(p2)
        db.commit()

    print("Database seeding complete. All Mock data injected.")

if __name__ == "__main__":
    seed_db()
