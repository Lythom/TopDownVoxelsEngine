namespace Server {
    public class UserData {
        public bool IsLogged;
        public ushort ShortId;

        public UserData(bool isLogged, ushort shortId) {
            IsLogged = isLogged;
            ShortId = shortId;
        }
    }
}