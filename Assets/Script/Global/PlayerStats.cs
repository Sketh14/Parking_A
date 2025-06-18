namespace Parking_A.Global
{
    [System.Serializable]
    public class PlayerStats
    {
        public int Coins;
        public int Gold;
        public int[] BoughtVehicleSkinIndexes;            //Only 32 skins for each vehicle | Using as flags to check if the skin is bought
        //  This will always be unique as the vehicle can only have 1 skin equipped, so not necessary for flags
        //  [OLD] Using as flags to check if the skin is equipped
        public byte[] EquippedVehicleSkinIndexes;

        public PlayerStats(int coins = 0, int gold = 0, int[] boughtIndexes = null, byte[] equippedIndexes = null)
        {
            Coins = coins;
            Gold = gold;
            BoughtVehicleSkinIndexes = boughtIndexes ?? new int[] { 1, 1, 1 };              //Default Skin Equipped
            EquippedVehicleSkinIndexes = equippedIndexes ?? new byte[] { 1, 1, 1 };              //Default Skin Equipped
        }
    }
}