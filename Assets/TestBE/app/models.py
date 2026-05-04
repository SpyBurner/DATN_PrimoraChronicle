from sqlalchemy import Column, String, Integer, Float, Boolean, DateTime, ForeignKey, Table
from sqlalchemy.orm import relationship
import uuid
from datetime import datetime
from .database import Base

def generate_uuid():
    return str(uuid.uuid4())

class BaseModel(Base):
    __abstract__ = True
    ID = Column(String, primary_key=True, default=generate_uuid)
    createdDateTime = Column(DateTime, default=datetime.utcnow)
    updatedDateTime = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    isDeleted = Column(Boolean, default=False)

class User(BaseModel):
    __tablename__ = "users"
    username = Column(String, unique=True, index=True)
    passwordHash = Column(String)
    xpTotal = Column(Integer, default=0)
    gold = Column(Integer, default=0)

    card_copies = relationship("CardCopy", back_populates="owner")
    decks = relationship("Deck", back_populates="owner")
    match_participations = relationship("MatchParticipant", back_populates="user")

class Card(BaseModel):
    __tablename__ = "cards"
    card_copies = relationship("CardCopy", back_populates="card")

class CardCopy(BaseModel):
    __tablename__ = "card_copies"
    userID = Column(String, ForeignKey("users.ID"))
    cardID = Column(String, ForeignKey("cards.ID"))
    
    owner = relationship("User", back_populates="card_copies")
    card = relationship("Card", back_populates="card_copies")

deck_cardcopy_association = Table(
    'deck_consists_of_cardcopy', Base.metadata,
    Column('deckID', String, ForeignKey('decks.ID')),
    Column('cardCopyID', String, ForeignKey('card_copies.ID'))
)

class Deck(BaseModel):
    __tablename__ = "decks"
    name = Column(String)
    description = Column(String)
    userID = Column(String, ForeignKey("users.ID"))
    
    owner = relationship("User", back_populates="decks")
    card_copies = relationship("CardCopy", secondary=deck_cardcopy_association)
    match_participations = relationship("MatchParticipant", back_populates="deck")

class ActionLog(BaseModel):
    __tablename__ = "action_logs"
    fileBucketURL = Column(String)
    match = relationship("Match", back_populates="action_log", uselist=False)

class Match(BaseModel):
    __tablename__ = "matches"
    endDateTime = Column(DateTime)
    actionLogID = Column(String, ForeignKey("action_logs.ID"))
    
    action_log = relationship("ActionLog", back_populates="match")
    participants = relationship("MatchParticipant", back_populates="match")

class MatchParticipant(BaseModel):
    __tablename__ = "match_participants"
    isWinner = Column(Boolean)
    goldReceived = Column(Integer)
    xpReceived = Column(Integer)
    deckID = Column(String, ForeignKey("decks.ID"))
    userID = Column(String, ForeignKey("users.ID"))
    championCardID = Column(String, ForeignKey("cards.ID"))
    matchID = Column(String, ForeignKey("matches.ID"))
    
    user = relationship("User", back_populates="match_participations")
    match = relationship("Match", back_populates="participants")
    deck = relationship("Deck", back_populates="match_participations")

champion_has_card_association = Table(
    'champion_has_card', Base.metadata,
    Column('championCardID', String, ForeignKey('cards.ID')),
    Column('cardID', String, ForeignKey('cards.ID'))
)

class SystemConfig(BaseModel):
    __tablename__ = "system_configs"
    dailyDealDiscountRate = Column(Float)
    championCardBasePrice = Column(Integer)
    commonCardBasePrice = Column(Integer)
    levelXpGrowthRate = Column(Float)
    startingLevelXp = Column(Integer)
    afkPenaltyAmount = Column(Integer)
