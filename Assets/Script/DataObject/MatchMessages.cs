using Mirror;

// Client → Server: muốn tìm trận
public struct MsgJoinQueue : NetworkMessage { }

// Client → Server: muốn tạo party
public struct MsgCreateParty : NetworkMessage { }

// Client → Server: muốn vào party bằng mã
public struct MsgJoinParty : NetworkMessage
{
    public string partyCode;
}

// Server → Client: phản hồi sau khi tạo party
public struct MsgPartyCreated : NetworkMessage
{
    public string partyCode;
}

// Server → Client: phản hồi lỗi
public struct MsgError : NetworkMessage
{
    public string message;
}
