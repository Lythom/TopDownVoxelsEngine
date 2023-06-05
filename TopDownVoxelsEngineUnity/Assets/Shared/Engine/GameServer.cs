namespace Shared
{
    public class GameServer
    {
        private GameState _state = new();

        public GameServer()
        {
            Character playerCharacter = new Character();
            playerCharacter.Position = new Vector3(1028f, 4.5f, 1028f);
            _state.Characters.Add(playerCharacter);

            LevelMap level = new LevelMap("server", "default");
        }
    }
}