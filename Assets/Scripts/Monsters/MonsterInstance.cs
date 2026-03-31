using System;

namespace MonsterWorldLike.Monsters
{
    [Serializable]
    public class MonsterInstance
    {
        public string monsterId;
        public long nextReadyUnix;

        public MonsterInstance(string id)
        {
            monsterId = id;
            nextReadyUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
