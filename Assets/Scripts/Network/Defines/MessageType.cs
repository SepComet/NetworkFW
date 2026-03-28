namespace Network.Defines
{
    public enum MessageType : byte
    {
        Unknow = 0,

        // Gameplay
        MoveInput = 1,
        PlayerState = 2,
        ShootInput = 3,
        CombatEvent = 4,
        PlayerJoin = 5,
        PlayerLeave = 6,
        PlayerAction = 7,
        GameState = 8,

        // Chat
        ChatMessage = 10,
        PrivateMessage = 11,
        SystemMessage = 12,

        // Session
        HeartBeat = 20,
        LoginRequest = 21,
        LoginResponse = 22,
        LogoutRequest = 23,

        // Room management
        CreateRoom = 30,
        JoinRoom = 31,
        LeaveRoom = 32,
        RoomList = 33,

        Heartbeat = 40,
        HeartbeatResponse = 41,
    }
}