using System;
using System.Collections.Generic;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts;
using SDTLevel = Artech.Genexus.Common.Parts.SDT.SDTLevel;
using SDTItem = Artech.Genexus.Common.Parts.SDT.SDTItem;

namespace Func4Genexus.Logic
{
    public class SdtGeneratorService
    {
        public SDT CreateOrUpdateSdt(Transaction trn, SDT existingSdt, string sdtName, HashSet<string> selectedAttributes, HashSet<string> selectedLevels)
        {
            var kbmodel = trn.Model;
            SDT sdt = existingSdt ?? new SDT(kbmodel);

            if (existingSdt == null)
            {
                sdt.Name = sdtName;
                sdt.Parent = trn.Parent;
            }

            // Map root level
            while (sdt.SDTStructure.Root.Items.Count > 0)
            {
                sdt.SDTStructure.Root.Items.RemoveAt(0);
            }

            PopulateSdtLevel(sdt.SDTStructure.Root, trn.Structure.Root, selectedAttributes, selectedLevels);

            sdt.Save();
            return sdt;
        }

        private void PopulateSdtLevel(SDTLevel sdtLevel, TransactionLevel trnLevel, HashSet<string> selectedAttributes, HashSet<string> selectedLevels)
        {
            foreach (var trnAttr in trnLevel.Attributes)
            {
                var attrName = trnAttr.Attribute.Name;
                if (selectedAttributes.Contains(attrName))
                {
                    SDTItem item = new SDTItem(sdtLevel.SDTStructure);
                    item.Name = attrName;
                    
                    item.Type = eDBType.GX_ATT_REF;
                    item.AttributeBasedOn = trnAttr.Attribute;
                    
                    item.Description = trnAttr.Attribute.Description;
                    sdtLevel.AddItem(item);
                }
            }

            foreach (var subLevel in trnLevel.Levels)
            {
                var levelName = subLevel.Name;
                if (selectedLevels.Contains(levelName))
                {
                    SDTLevel newSdtLevel = new SDTLevel(sdtLevel.SDTStructure);
                    newSdtLevel.Name = levelName;
                    newSdtLevel.IsCollection = true;
                    sdtLevel.AddLevel(newSdtLevel);
                    
                    PopulateSdtLevel(newSdtLevel, subLevel, selectedAttributes, selectedLevels);
                }
            }
        }
    }
}
