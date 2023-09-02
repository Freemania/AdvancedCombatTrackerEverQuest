﻿#region Complexi's Region
using Advanced_Combat_Tracker;
using EverQuestDPSPlugin.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Windows.Forms;

namespace EverQuestDPSPlugin
{
    public partial class EverQuestDPSPlugin : IEverQuestDPSPlugin
    {
        #region Class Members 2
        private ConcurrentQueue<MasterSwing> masterSwingsQueue = new ConcurrentQueue<MasterSwing>();
        private bool isProcessing = false;
        delegate void matchParse(Match regexMatch);
        List<Tuple<Color, Regex>> regexTupleList;
        Regex selfCheck;
        Regex possesive;
        Regex tellsregex;
        bool nonMatchVisible = false; //for keep track of whether or not the non matching form is displayed
        bool populationVariance; //for keeping track of whether population variance or sample variance is displayed
        nonmatch nm; //Form for non regex matching log lines
        string settingsFile;
        SettingsSerializer xmlSettings;
        object varianceChkBxLockObject = new object(), nonMatchChkBxLockObject = new object();
        #endregion

        internal DateTime ParseDateTime(String timeStamp)
        {
            DateTime.TryParseExact(timeStamp, EverQuestDPSPluginResource.eqDateTimeStampFormat, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AssumeLocal, out DateTime currentEQTimeStamp);
            return currentEQTimeStamp;
        }

        internal String RegexString(String regex)
        {
            if (regex == null)
                throw new ArgumentNullException("Missing value for regex");
            else
                return $@"\[(?<{EverQuestDPSPluginResource.dateTimeOfLogLineString}>.+)\] {regex}";
        }

        internal void PopulateRegexNonCombat()
        {
            possesive = new Regex(@"(?:[`']s\s)(?<" + $@"{EverQuestDPSPluginResource.possesiveOf}" + @">\S[^']+)(?:[']s\s(?<" + $@"{EverQuestDPSPluginResource.secondaryPossesiveOf}" + @">\S+)){0,1}", RegexOptions.Compiled);
            tellsregex = new Regex(RegexString(EverQuestDPSPluginResource.tellsRegex), RegexOptions.Compiled);
            selfCheck = new Regex(EverQuestDPSPluginResource.selfMatch, RegexOptions.Compiled);
            regexTupleList = new List<Tuple<Color, Regex>>();
        }

        private void PopulateRegexCombat()
        {
            String MeleeAttack = @"(?<attacker>.+) (?<attackType>" + $@"{EverQuestDPSPluginResource.attackTypes}" + @")(|s|es|bed) (?<victim>.+) for (?<damageAmount>[\d]+) (?:(?:point)(?:s|)) of damage.(?:\s\((?<damageSpecial>.+)\)){0,1}";
            String Evasion = @"(?<attacker>.*) tries to (?<attackType>\S+) (?:(?<victim>(.+)), but \1) (?:(?<evasionType>" + $@"{EverQuestDPSPluginResource.evasionTypes}" + @"))(?:\swith (your|his|hers|its) (shield|staff)){0,1}!(?:[\s][\(](?<evasionSpecial>.+)[\)]){0,1}";
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Clear();
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Red, new Regex(RegexString(MeleeAttack), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.ForestGreen, new Regex(RegexString(EverQuestDPSPluginResource.DamageShield), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Plum, new Regex(RegexString(EverQuestDPSPluginResource.MissedMeleeAttack), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Goldenrod, new Regex(RegexString(EverQuestDPSPluginResource.SlainMessage), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Red, new Regex(RegexString(EverQuestDPSPluginResource.SpellDamage), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Maroon, new Regex(RegexString(EverQuestDPSPluginResource.ZoneChange), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.DarkBlue, new Regex(RegexString(EverQuestDPSPluginResource.Heal), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.Silver, new Regex(RegexString(EverQuestDPSPluginResource.Unknown), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.DeepSkyBlue, new Regex(RegexString(Evasion), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.LightBlue, new Regex(RegexString(EverQuestDPSPluginResource.Banestrike), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
            regexTupleList.Add(new Tuple<Color, Regex>(Color.AliceBlue, new Regex(RegexString(EverQuestDPSPluginResource.SpellDamageOverTime), RegexOptions.Compiled)));
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(regexTupleList.Count, regexTupleList[regexTupleList.Count - 1].Item1);
        }

        private void FormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            bool match = false;
            for (int i = 0; i < regexTupleList.Count; i++)
            {
                Match regexMatch = regexTupleList[i].Item2.Match(logInfo.logLine);
                if (regexMatch.Success)
                {
                    match = true;
                    logInfo.detectedType = i + 1;
                    ParseEverQuestLogLine(regexMatch, i + 1);
                    break;
                }
            }
            Match tellmatch = tellsregex.Match(logInfo.logLine);
            if (!match && !tellmatch.Success)
            {
                AddLogLineToNonMatch(logInfo.logLine);
            }
        }

        EverQuestSwingType GetDamageShieldType(String damageShieldType)
        {
            List<String> damageShieldTypes = new List<String>()
            {
                "flames",
                "frost",
                "thorns"
            };
            return (damageShieldTypes.Select((value) =>
            {
                return value == damageShieldType;
            }).Count() > 0) ? EverQuestSwingType.DamageShield : 0;
        }

        internal Tuple<EverQuestSwingType, String> GetTypeAndNameForPet(String nameToSetTypeTo)
        {
            Match possessiveMatch = possesive.Match(nameToSetTypeTo);
            EverQuestSwingType everQuestSwingType = 0;
            if (possessiveMatch.Success)
            {
                if (possessiveMatch.Groups[EverQuestDPSPluginResource.secondaryPossesiveOf].Success)
                {
                    everQuestSwingType = GetDamageShieldType(possessiveMatch.Groups[EverQuestDPSPluginResource.secondaryPossesiveOf].Value);
                }
                switch (possessiveMatch.Groups[EverQuestDPSPluginResource.possesiveOf].Value)
                {
                    case "pet":
                        return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.Pet | everQuestSwingType, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                    case "warder":
                        return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.Warder | everQuestSwingType, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                    case "ward":
                        return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.Ward | everQuestSwingType, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                    case "familiar":
                        return new Tuple<EverQuestSwingType, String>(EverQuestSwingType.Familiar | everQuestSwingType, nameToSetTypeTo.Substring(0, possessiveMatch.Index));
                    default:
                        break;
                }
                everQuestSwingType = GetDamageShieldType(possessiveMatch.Groups[EverQuestDPSPluginResource.possesiveOf].Value);
                return new Tuple<EverQuestSwingType, string>(everQuestSwingType, nameToSetTypeTo);
            }
            else return new Tuple<EverQuestSwingType, string>(everQuestSwingType, nameToSetTypeTo);
        }

        internal bool CheckIfSelf(String nameOfCharacter)
        {
            Regex regexSelf = new Regex(@"((it|her|him|them)(s|sel(f|ves)))", RegexOptions.Compiled);
            return regexSelf.Match(nameOfCharacter).Success;
        }

        private MasterSwing ParseMasterSwing(EverQuestSwingType attackTypeToCombine,
            Match logLineRegexMatch,
            String character1GroupName,
            String character2GroupName,
            String specialGroupName,
            Dnum dnumValue,
            String attackTypeGroupName,
            String damageType
            )
        {
            Tuple<EverQuestSwingType, String> attackerAndTypeMelee = GetTypeAndNameForPet(logLineRegexMatch.Groups[character1GroupName].Value);
            Tuple<EverQuestSwingType, String> victimAndTypeMelee = GetTypeAndNameForPet(logLineRegexMatch.Groups[character2GroupName].Value);
            String attacker, victim;
            EverQuestSwingType everQuestSwingTypeToParseMelee = 0;
            if (attackerAndTypeMelee.Item1.HasFlag(EverQuestSwingType.Pet) || victimAndTypeMelee.Item1.HasFlag(EverQuestSwingType.Pet))
                everQuestSwingTypeToParseMelee |= EverQuestSwingType.Pet;
            else if (attackerAndTypeMelee.Item1.HasFlag(EverQuestSwingType.Warder) || victimAndTypeMelee.Item1.HasFlag(EverQuestSwingType.Warder))
                everQuestSwingTypeToParseMelee |= EverQuestSwingType.Warder;
            else if (attackerAndTypeMelee.Item1.HasFlag(EverQuestSwingType.Ward) || victimAndTypeMelee.Item1.HasFlag(EverQuestSwingType.Ward))
                everQuestSwingTypeToParseMelee |= EverQuestSwingType.Ward;
            else if (attackerAndTypeMelee.Item1.HasFlag(EverQuestSwingType.NonMelee) || victimAndTypeMelee.Item1.HasFlag(EverQuestSwingType.NonMelee))
                everQuestSwingTypeToParseMelee |= EverQuestSwingType.DamageShield;

            if (everQuestSwingTypeToParseMelee.HasFlag(EverQuestSwingType.DamageShield))
            {
                attacker = CheckIfSelf(victimAndTypeMelee.Item2) ? CharacterNamePersonaReplace(attackerAndTypeMelee.Item2) : CharacterNamePersonaReplace(victimAndTypeMelee.Item2);
                victim = CharacterNamePersonaReplace(attackerAndTypeMelee.Item2);
            }
            else
            {
                attacker = CharacterNamePersonaReplace(attackerAndTypeMelee.Item2);
                victim = CheckIfSelf(victimAndTypeMelee.Item2) ? CharacterNamePersonaReplace(attackerAndTypeMelee.Item2) : CharacterNamePersonaReplace(victimAndTypeMelee.Item2);
            }
            MasterSwing masterSwingMelee = new MasterSwing((everQuestSwingTypeToParseMelee | attackTypeToCombine).GetEverQuestSwingTypeExtensionIntValue()
            , logLineRegexMatch.Groups[specialGroupName].Success && logLineRegexMatch.Groups[specialGroupName].Value.Contains(EverQuestDPSPluginResource.Critical)
            , logLineRegexMatch.Groups[specialGroupName].Success ? logLineRegexMatch.Groups[specialGroupName].Value : String.Empty
            , dnumValue
            , ParseDateTime(logLineRegexMatch.Groups[EverQuestDPSPluginResource.dateTimeOfLogLineString].Value)
            , ActGlobals.oFormActMain.GlobalTimeSorter
            , logLineRegexMatch.Groups[attackTypeGroupName].Value
            , attacker
            , damageType
            , victim);
            masterSwingMelee.Tags.Add("lastEstimatedTime", ActGlobals.oFormActMain.LastEstimatedTime);
            return masterSwingMelee;
        }

        private void ParseEverQuestLogLine(Match regexMatch, int logMatched)
        {
            switch (logMatched)
            {
                //Melee attack
                case 1:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damageAmount"].Value));
                        MasterSwing masterSwingMelee = ParseMasterSwing(
                            EverQuestSwingType.Melee,
                            regexMatch,
                            "attacker",
                            "victim",
                            "damageSpecial",
                            damage,
                            "attackType",
                            "Hitpoints"
                        );
                        EnqueueCombatAction(masterSwingMelee);
                    }
                    break;
                //Non-melee damage shield
                case 2:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value));
                        MasterSwing masterSwingDamageShield = ParseMasterSwing(
                                EverQuestSwingType.NonMelee,
                                regexMatch,
                                "attacker",
                                "victim",
                                "damageSpecial",
                                damage,
                                "damageShieldDamageType",
                                "Hitpoints");
                        EnqueueCombatAction(masterSwingDamageShield);
                    }
                    break;
                //Missed melee
                case 3:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum miss = new Dnum(Dnum.Miss);
                        MasterSwing masterSwingMissedMelee = ParseMasterSwing(
                                EverQuestSwingType.Melee,
                                regexMatch,
                                "attacker",
                                "victim",
                                "damageSpecial",
                                miss,
                                "attackType",
                                "Miss"
                            );
                        EnqueueCombatAction(masterSwingMissedMelee);
                    }
                    break;
                //Death message
                case 4:
                    MasterSwing masterSwingSlain = new MasterSwing(0, false, new Dnum(Dnum.Death), ActGlobals.oFormActMain.LastEstimatedTime, ActGlobals.oFormActMain.GlobalTimeSorter, String.Empty, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), String.Empty, CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value));
                    EnqueueCombatAction(masterSwingSlain);
                    break;
                //Spell Cast
                case 5:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value), regexMatch.Groups["typeOfDamage"].Value);
                        MasterSwing masterSwingSpellcast = ParseMasterSwing(
                                EverQuestSwingType.DirectDamageSpell,
                                regexMatch,
                                "attacker",
                                "victim",
                                "spellSpecial",
                                damage,
                                "damageEffect",
                                "Hitpoints"
                            );
                        EnqueueCombatAction(masterSwingSpellcast);
                    }
                    break;
                case 6:
                    //when checking the HistoryRecord the EndTime should be compared against default(DateTime) to determine if it an exact value among other methods such does the default(DateTime) take place before the StartTime for the HistoryRecord
                    //ActGlobals.oFormActMain.ZoneDatabaseAdd(new HistoryRecord(0, ActGlobals.oFormActMain.LastKnownTime, new DateTime(), regexMatch.Groups["zoneName"].Value != String.Empty ? regexMatch.Groups["zoneName"].Value : throw new Exception("Zone regex triggered but zone name not found."), ActGlobals.charName));
                    ActGlobals.oFormActMain.ChangeZone(regexMatch.Groups["zoneName"].Value);
                    break;
                //heal
                case 7:
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        MasterSwing masterSwingHeal = ParseMasterSwing(
                                regexMatch.Groups["overTime"].Success ? EverQuestSwingType.HealOverTime : EverQuestSwingType.InstantHealing,
                                regexMatch,
                                "healer",
                                "healingTarget",
                                "healingSpecial",
                                new Dnum(Int64.Parse(regexMatch.Groups["healingPoints"].Value)),
                                "healingSpell",
                                "Hitpoints"
                            );
                        if (regexMatch.Groups["overHealPoints"].Success)
                            masterSwingHeal.Tags["overheal"] = Int64.Parse(regexMatch.Groups["overHealPoints"].Value);
                        EnqueueCombatAction(masterSwingHeal);
                    }
                    break;
                case 8:
                    MasterSwing masterSwingUnknown = new MasterSwing(EverQuestSwingType.NonMelee.GetEverQuestSwingTypeExtensionIntValue(), false, new Dnum(Dnum.Unknown)
                    {
                        DamageString2 = regexMatch.Value
                    }, ParseDateTime(regexMatch.Groups["dateTimeOfLogLine"].Value), ActGlobals.oFormActMain.GlobalTimeSorter, "Unknown", "Unknown", "Unknown", "Unknown");
                    EnqueueCombatAction(masterSwingUnknown);
                    break;
                case 9:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        MasterSwing masterSwingEvasion = ParseMasterSwing(
                                EverQuestSwingType.Melee,
                                regexMatch,
                                "attacker",
                                "victim",
                                "evasionSpecial",
                                new Dnum(Dnum.NoDamage, regexMatch.Groups["evasionType"].Value),
                                "attackType",
                                "Hitpoints"
                            );
                        EnqueueCombatAction(masterSwingEvasion);
                    }
                    break;
                case 10:
                    if (ActGlobals.oFormActMain.SetEncounter(ActGlobals.oFormActMain.LastKnownTime, CharacterNamePersonaReplace(regexMatch.Groups["attacker"].Value), CharacterNamePersonaReplace(regexMatch.Groups["victim"].Value)))
                    {
                        Dnum damage = new Dnum(Int64.Parse(regexMatch.Groups["damagePoints"].Value));
                        MasterSwing masterSwingSpellcast = ParseMasterSwing(
                                EverQuestSwingType.DamageOverTimeSpell,
                                regexMatch,
                                "attacker",
                                "victim",
                                "spellSpecial",
                                damage,
                                "damageEffect",
                                "Hitpoints"
                            );
                        EnqueueCombatAction(masterSwingSpellcast);
                    }
                    break;
                default:
                    break;
            }
        }

        private string CharacterNamePersonaReplace(string PersonaString)
        {
            return selfCheck.Match(PersonaString).Success ? ActGlobals.charName : PersonaString;
        }

        #region Statistic processing

        //Variance calculation for attack damage
        private double AttackTypeGetVariance(AttackType Data)
        {
            List<MasterSwing> ms = Data.Items.ToList().Where((item) => item.Damage.Number >= 0).ToList();
            double average;
            lock (varianceChkBxLockObject)
            {
                if (!populationVariance && Data.Items.Count > 1)
                {
                    average = ms.Select((item) => item.Damage.Number).Average();
                    return ms.Sum((item) =>
                    {
                        return Math.Pow(average - item.Damage.Number, 2.0);
                    }) / (ms.Count - 1);
                }
                else if (populationVariance && Data.Items.Count > 0)
                {
                    average = ms.Select((item) => item.Damage.Number).Average();
                    return ms.Sum((item) =>
                    {
                        return Math.Pow(average - item.Damage.Number, 2.0);
                    }) / ms.Count;
                }
                else
                    return default;
            }
        }
        #endregion

        private Color GetSwingTypeColor(EverQuestSwingType eqst)
        {
            switch (eqst)
            {
                case EverQuestSwingType.Melee:
                    return Color.DarkViolet;
                case EverQuestSwingType.NonMelee:
                    return Color.DarkSlateGray;
                case EverQuestSwingType.InstantHealing:
                    return Color.DodgerBlue;
                case EverQuestSwingType.HealOverTime:
                    return Color.GreenYellow;
                case EverQuestSwingType.Bane:
                    return Color.Honeydew;
                case EverQuestSwingType.WardInstantHealing:
                    return Color.LemonChiffon;
                case EverQuestSwingType.WardHealOverTime:
                    return Color.LightSeaGreen;
                case EverQuestSwingType.DamageOverTimeSpell:
                    return Color.Olive;
                case EverQuestSwingType.DirectDamageSpell:
                    return Color.SandyBrown;
                case EverQuestSwingType.PetMelee:
                    return Color.Violet;
                case EverQuestSwingType.PetNonMelee:
                    return Color.CornflowerBlue;
                case EverQuestSwingType.WarderMelee:
                    return Color.MistyRose;
                case EverQuestSwingType.WarderNonMelee:
                    return Color.GreenYellow;
                case EverQuestSwingType.WarderDirectDamageSpell:
                    return Color.ForestGreen;
                case EverQuestSwingType.WarderDamageOverTimeSpell:
                    return Color.Thistle;
                case EverQuestSwingType.DamageShield:
                    return Color.Green;
                default:
                    return Color.Black;
            }
        }

        #region User Interface Update code
        private void ChangeLblStatus(String status)
        {

            switch (lblStatus.InvokeRequired)
            {
                case true:
                    this.lblStatus.Invoke(new Action(() =>
                    {
                        this.lblStatus.Text = status;
                    }));
                    break;
                case false:
                    this.lblStatus.Text = status;
                    break;
                default:
                    break;
            }
        }

        public void ChangeNonmatchFormCheckBox(bool Checked)
        {
            if (nonMatchVisibleChkbx.InvokeRequired)
            {
                nonMatchVisibleChkbx.Invoke(new Action(() =>
                {
                    nonMatchVisibleChkbx.Checked = Checked;
                }));
            }
            else
            {
                nonMatchVisibleChkbx.Checked = Checked;
            }
        }

        private void AddLogLineToNonMatch(String message)
        {
            if (nm.InvokeRequired)
            {
                nm.Invoke(new Action(() =>
                {
                    nm.AddLogLineToForm(message);
                }));
            }
            else
                nm.AddLogLineToForm(message);
        }
        //checkbox processing event for population or sample variance
        private void VarianceChkBx_CheckedChanged(object sender, EventArgs e)
        {
            var checkBx = sender as CheckBox;
            lock (varianceChkBxLockObject)
                this.populationVariance = checkBx.Checked;
            switch (this.populationVariance)
            {
                case true:
                    ChangeLblStatus($"Reporting population variance {EverQuestDPSPluginResource.pluginName}");
                    break;
                case false:
                    ChangeLblStatus($"Reporting sample variance {EverQuestDPSPluginResource.pluginName}");
                    break;
            }
        }

        private void NonMatchVisible_CheckedChanged(object sender, EventArgs e)
        {
            var checkBx = sender as CheckBox;
            lock (nonMatchChkBxLockObject)
                this.nonMatchVisible = checkBx.Checked;
            if (nm == null || nm.IsDisposed)
            {
                nm = new nonmatch(this);
            }
            if (nm.InvokeRequired)
            {
                checkBx.Invoke(new Action(() =>
                {
                    checkBx.Enabled = false;
                }));
            }
            else
            {
                checkBx.Enabled = false;
            }
            switch (this.nonMatchVisible)
            {
                case true:

                    if (this.nm.InvokeRequired)
                        nm.Invoke(new Action(() =>
                        {
                            nm.Visible = this.nonMatchVisible;
                        }));
                    else
                        nm.Visible = this.nonMatchVisible;
                    break;
                case false:
                    if (this.nm.InvokeRequired)
                        nm.Invoke(new Action(() =>
                        {
                            nm.Visible = this.nonMatchVisible;
                        }));
                    else
                        nm.Visible = this.nonMatchVisible;
                    break;
                default:
                    break;
            }
            if (nm.InvokeRequired)
            {
                checkBx.Invoke(new Action(() =>
                {
                    checkBx.Enabled = true;
                }));
            }
            else
            {
                checkBx.Enabled = true;
            }
        }
        #endregion

        private string AttackTypeGetCritTypes(AttackType Data)
        {
            List<MasterSwing> ms = Data.Items.ToList().Where((item) => item.Damage >= 0).ToList();
            int CripplingBlowCount = 0;
            int LockedCount = 0;
            int CriticalCount = 0;
            int StrikethroughCount = 0;
            int RiposteCount = 0;
            int NonDefinedCount = 0;
            int FlurryCount = 0;
            int LuckyCount = 0;
            int DoubleBowShotCount = 0;
            int TwincastCount = 0;
            int WildRampageCount = 0;
            int FinishingBlowCount = 0;
            int count = ms.Count;

            FinishingBlowCount = ms.Where((finishingBlow) =>
            {
                return finishingBlow.Special.Contains(EverQuestDPSPluginResource.FinishingBlow);
            }).Count();
            CriticalCount = ms.Where((critital) =>
            {
                return critital.Critical;
            }).Count();
            FlurryCount = ms.Where((flurry) =>
            {
                return flurry.Special.Contains(EverQuestDPSPluginResource.Flurry);
            }).Count();
            LuckyCount = ms.Where((lucky) =>
            {
                return lucky.Special.Contains(EverQuestDPSPluginResource.Lucky);
            }).Count();
            CripplingBlowCount = ms.Where((cripplingBlow) =>
            {
                return cripplingBlow.Special.Contains(EverQuestDPSPluginResource.CripplingBlow);
            }).Count();
            LockedCount = ms.Where((locked) =>
            {
                return locked.Special.Contains(EverQuestDPSPluginResource.Locked);
            }).Count();
            StrikethroughCount = ms.Where((srikethrough) =>
            {
                return srikethrough.Special.Contains(EverQuestDPSPluginResource.Strikethrough);
            }).Count();
            RiposteCount = ms.Where((riposte) =>
            {
                return riposte.Special.Contains(EverQuestDPSPluginResource.Riposte);
            }).Count();
            DoubleBowShotCount = ms.Where((doubleBowShot) =>
            {
                return doubleBowShot.Special.Contains(EverQuestDPSPluginResource.DoubleBowShot);
            }).Count();
            TwincastCount = ms.Where((twincast) =>
            {
                return twincast.Special.Contains(EverQuestDPSPluginResource.Twincast);
            }).Count();
            WildRampageCount = ms.Where((twincast) =>
            {
                return twincast.Special.Contains(EverQuestDPSPluginResource.WildRampage);
            }).Count();
            NonDefinedCount = ms.Where((nondefined) =>
            {
                return !nondefined.Special.Contains(EverQuestDPSPluginResource.Twincast) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.DoubleBowShot) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Riposte) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.CripplingBlow) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Lucky) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Flurry) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Critical) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.WildRampage) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.CripplingBlow) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.Strikethrough) &&
                    !nondefined.Special.Contains(EverQuestDPSPluginResource.FinishingBlow)
                    && nondefined.Special.Length > ActGlobals.ActLocalization.LocalizationStrings["specialAttackTerm-none"].DisplayedText.Length;

            }).Count();

            float CripplingBlowPerc = ((float)CripplingBlowCount / (float)count) * 100f;
            float LockedPerc = ((float)LockedCount / (float)count) * 100f;
            float CriticalPerc = ((float)CriticalCount / (float)count) * 100f;
            float NonDefinedPerc = ((float)NonDefinedCount / (float)count) * 100f;
            float StrikethroughPerc = ((float)StrikethroughCount / (float)count) * 100f;
            float RipostePerc = ((float)RiposteCount / (float)count) * 100f;
            float FlurryPerc = ((float)FlurryCount / (float)count) * 100f;
            float LuckyPerc = ((float)LuckyCount / (float)count) * 100f;
            float DoubleBowShotPerc = ((float)DoubleBowShotCount / (float)count) * 100f;
            float TwincastPerc = ((float)TwincastCount / (float)count) * 100f;
            float WildRampagePerc = ((float)WildRampageCount / (float)count) * 100f;
            float FinishingBlowPerc = ((float)FinishingBlowCount / (float)count) * 100f;

            return $"{CripplingBlowPerc:000.0}%CB-{LockedPerc:000.0}%Locked-{CriticalPerc:000.0}%C-{StrikethroughPerc:000.0}%S-{RipostePerc:000.0}%R-{FlurryPerc:000.0}%F-{LuckyPerc:000.0}%Lucky-{DoubleBowShotPerc:000.0}%DB-{TwincastPerc:000.0}%TC-{WildRampagePerc:000.0}%WR-{FinishingBlowPerc:000.0}%FB-{NonDefinedPerc:000.0}%ND";
        }

        private void EnqueueCombatAction(MasterSwing ms)
        {
            masterSwingsQueue.Enqueue(ms);
            if (!isProcessing)
            {
                isProcessing = true;
                new Task(() =>
                {
                    MasterSwing masterSwing = default;
                    while (!masterSwingsQueue.IsEmpty)
                    {
                        try
                        {
                            if (masterSwingsQueue.TryDequeue(out masterSwing))
                            {
                                ActGlobals.oFormActMain.AddCombatAction(masterSwing);
                            }
                        }
                        catch (Exception ex)
                        {
                            ActGlobals.oFormActMain.WriteExceptionLog(ex, "while trying to dequeue and write to combat action");
                        }
                    }
                    isProcessing = false;
                }).Start();
            }
        }
    }
}
#endregion