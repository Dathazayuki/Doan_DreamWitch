using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.Systems.Facility
{
    [CreateAssetMenu(fileName = "FacilityUpgradeDatabase", menuName = "DreamKnight/Facility/Upgrade Database")]
    public class FacilityUpgradeDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<FacilityUpgradeSO> upgrades = new List<FacilityUpgradeSO>();

        public IReadOnlyList<FacilityUpgradeSO> Upgrades => upgrades;

        public FacilityUpgradeSO FindById(string upgradeId)
        {
            if (string.IsNullOrWhiteSpace(upgradeId) || upgrades == null)
                return null;

            for (int i = 0; i < upgrades.Count; i++)
            {
                FacilityUpgradeSO upgrade = upgrades[i];
                if (upgrade != null && upgrade.UpgradeId == upgradeId)
                    return upgrade;
            }

            return null;
        }
    }
}
