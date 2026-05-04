from pydantic import BaseModel, ConfigDict
from typing import List, Optional
from datetime import datetime

class UserBase(BaseModel):
    username: str

class UserCreate(UserBase):
    password: str

class UserResponse(UserBase):
    ID: str
    xpTotal: int
    gold: int
    model_config = ConfigDict(from_attributes=True)

class LoginRequest(BaseModel):
    username: str
    password: str

class CardCopyResponse(BaseModel):
    ID: str
    cardID: str
    model_config = ConfigDict(from_attributes=True)

class DeckCreate(BaseModel):
    name: str
    description: str
    cardCopyIDs: List[str]

class DeckResponse(BaseModel):
    ID: str
    name: str
    description: str
    cardCopyIDs: List[str]
    model_config = ConfigDict(from_attributes=True)

class MatchResultSubmit(BaseModel):
    winnerUserID: str
    loserUserID: str
    winnerDeckID: str
    loserDeckID: str
    actionLogData: dict

class MatchHistoryResponse(BaseModel):
    matchID: str
    endDateTime: datetime
    isWinner: bool
    goldReceived: int
    xpReceived: int
    actionLogURL: str
    model_config = ConfigDict(from_attributes=True)
