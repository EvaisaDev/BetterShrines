using System;
using System.Collections.Generic;
using System.Text;

namespace Evaisa.BetterShrines
{
    internal class Tokens
    {
        internal static void Init()
        {
            LanguageAPI.Add("SHRINE_CHANCE_PUNISHED_MESSAGE", "<style=cShrine>{0} offered to the shrine and was punished!</color>");
            LanguageAPI.Add("SHRINE_CHANCE_PUNISHED_MESSAGE_2P", "<style=cShrine>You offer to the shrine and are punished!</color>");
            LanguageAPI.Add("SHRINE_IMP_MESSAGE", "<style=cShrine>{0} inspected the vase and tiny imps appeared!</color>");
            LanguageAPI.Add("SHRINE_IMP_MESSAGE_2P", "<style=cShrine>You inspected the vase and tiny imps appeared!</color>");
            LanguageAPI.Add("SHRINE_IMP_COMPLETED", "<style=cIsHealing>You killed all the imps and found some items!</color>");
            LanguageAPI.Add("SHRINE_IMP_COMPLETED_2P", "<style=cIsHealing>{0} killed all the imps and found some items!</color>");
            LanguageAPI.Add("SHRINE_IMP_FAILED", "<style=cIsHealth>You failed to kill all the imps in time!</color>");
            LanguageAPI.Add("SHRINE_IMP_FAILED_2P", "<style=cIsHealth>{0} failed to kill all the imps in time!</color>");
            LanguageAPI.Add("SHRINE_IMP_NAME", "Shrine of Imps");
            LanguageAPI.Add("SHRINE_IMP_CONTEXT", "Inspect the vase.");
            LanguageAPI.Add("SHRINE_FALLEN_NAME", "Shrine of the Fallen");
            LanguageAPI.Add("SHRINE_FALLEN_CONTEXT", "Offer to Shrine of the Fallen.");
            LanguageAPI.Add("SHRINE_FALLEN_USED", "<style=cIsHealing>{0} offered to the Shrine of the Fallen and revived {1}!</color>");
            LanguageAPI.Add("SHRINE_FALLEN_USED_2P", "<style=cIsHealing>You offer to the Shrine of the Fallen and revived {1}!</color>");
            LanguageAPI.Add("OBJECTIVE_KILL_TINY_IMPS", "Kill the <color={0}>tiny imps</color> ({1}/{2}) in {3} seconds!");
        }
    }
}
