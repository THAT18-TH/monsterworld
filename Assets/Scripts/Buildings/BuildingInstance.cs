using System;

namespace MonsterWorldLike.Buildings
{
    [Serializable]
    public class BuildingInstance
    {
        public string buildingId;
        public string areaId = "main";
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public long placedUnix;
        public long readyUnix;
        public bool completed;
    }
}
