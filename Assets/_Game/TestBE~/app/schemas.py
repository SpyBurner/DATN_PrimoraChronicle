from pydantic import BaseModel, ConfigDict
from typing import List, Optional
from datetime import datetime
from uuid import UUID

class UserBase(BaseModel):
    username: str

class UserCreate(UserBase):
    password: str

class UserResponse(UserBase):
    ID: UUID
    xpTotal: int
    gold: int
    model_config = ConfigDict(from_attributes=True)

class UserBriefResponse(BaseModel):
    ID: UUID
    username: str
    gold: int
    xpTotal: Optional[int] = None

class LoginRequest(BaseModel):
    username: str
    password: str

class CardCopyResponse(BaseModel):
    ID: UUID
    cardID: UUID
    StringID: Optional[str] = None
    model_config = ConfigDict(from_attributes=True)

class DeckSummary(BaseModel):
    id: UUID
    name: str

class DeckSummaryListResponse(BaseModel):
    decks: List[DeckSummary]

class DeckDetail(BaseModel):
    id: UUID
    name: str
    cardIds: List[str]

class DeckSaveRequest(BaseModel):
    id: UUID
    name: str
    cardIds: List[str]

class DeckCreate(BaseModel):
    name: str
    description: str
    cardCopyIDs: List[UUID]

class DeckResponse(BaseModel):
    ID: UUID
    name: str
    description: str
    cardCopyIDs: List[UUID]
    model_config = ConfigDict(from_attributes=True)

class MatchResultSubmit(BaseModel):
    winnerUserID: UUID
    loserUserID: UUID
    winnerDeckID: UUID
    loserDeckID: UUID
    actionLogData: dict

class MatchHistoryResponse(BaseModel):
    matchID: UUID
    endDateTime: datetime
    isWinner: bool
    goldReceived: int
    xpReceived: int
    actionLogURL: str
    model_config = ConfigDict(from_attributes=True)
