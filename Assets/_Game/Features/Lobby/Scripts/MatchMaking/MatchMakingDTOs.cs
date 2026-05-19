using System;

[Serializable]
public class QueueJoinResponse
{
    public string status;
    public string queued_at;
}

[Serializable]
public class QueueStatusResponse
{
    public string status;
    public string session_name;
}
