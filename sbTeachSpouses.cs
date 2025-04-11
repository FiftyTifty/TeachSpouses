using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Extensions;
using System.Linq;
using System.Runtime.CompilerServices;


namespace TeachSpouses
{

    public class tsCharacterToTeach
    {
        public Hero Student;
        public int NumLevelsCanBeTaught;
    }

    public class tsTeachingSkill
    {
        public SkillObject Skill;
        public List<tsCharacterToTeach> Students;
    }


    class Classroom
    {
        public List<tsTeachingSkill> Teaching;
        public static Classroom Instance { get; } = new Classroom
        {

            Teaching = new List<tsTeachingSkill>()

        };

        public static void ClearTeachingData()
        {

            int iCounter;
            int iSubCounter;
            tsTeachingSkill tsTeachingCurrent;

            for (iCounter = Classroom.Instance.Teaching.Count - 1; iCounter >= 0; iCounter--)
            {

                tsTeachingCurrent = Classroom.Instance.Teaching[iCounter];

                for (iSubCounter = tsTeachingCurrent.Students.Count - 1; iCounter >= 0; iCounter--)
                {
                    tsTeachingCurrent.Students[iSubCounter] = null;
                }

                tsTeachingCurrent.Students.Clear();
                tsTeachingCurrent = null;

            }

        }

    }

    public static class tsSettings
    {

        //Base teaching cost for 1 skill point
        public const float tsBase = 25;
        //Above this skill level, costs increase per level
        public const float tsThresholdLvl = 25;

        //Matchmaking multipliers
        public const float MMSixties = 0.2F;
        public const float MMFifties = 0.5F;
        public const float MMForties = 0.7F;
        public const float MMAny = 1.3F;
        public const float MMThirties = 1.5F;
        public const float MMTwenties = 2.0F;

        //Flat prices
        public const int MMfSixties = 25;
        public const int MMfFifties = 50;
        public const int MMfForties = 100;
        public const int MMfAny = 500;
        public const int MMfThirties = 400;
        public const int MMfTwenties = 800;

        public static int CalculateTeachingPrice(int CurrentLevel, int NumLevelsToTeach)
        {
            
            return (int)( tsBase * (NumLevelsToTeach * ( (CurrentLevel + NumLevelsToTeach) * 0.01)) );

        }

        public static TextObject CreateTextObject(string strToAdd, List<string> listVars, Object[] arrayValues)
        {
            TextObject toCurrent = new TextObject(strToAdd);
            
            for (int iCounter = 0; iCounter < listVars.Count - 1; iCounter++)
            {

                if (arrayValues[iCounter].GetType() == typeof(string))
                {
                    toCurrent.SetTextVariable(listVars[iCounter], (string)arrayValues[iCounter]);
                }
                else if (arrayValues[iCounter].GetType() == typeof(int))
                {
                    toCurrent.SetTextVariable(listVars[iCounter], (int)arrayValues[iCounter]);
                }
                else if (arrayValues[iCounter].GetType() == typeof(float))
                {
                    toCurrent.SetTextVariable(listVars[iCounter], (float)arrayValues[iCounter]);
                }

            }

            return toCurrent;

        }

    }

    public class TeachSpousesMenus
    {

        public static void DoMatchmakerButtonAgeRange(MenuCallbackArgs x, int iMinAge, int iMaxAge, int iPrice)
        {

            MBList<Hero> listSpouse = Util.GetUnwedHeroesInKingdom(Settlement.CurrentSettlement.OwnerClan.Kingdom);
            Util.FilterMarryListByAge(listSpouse, iMinAge, iMaxAge);

            Hero heroLearn = listSpouse.GetRandomElement();
            heroLearn.IsKnownToPlayer = true;

            listSpouse = null;
            Util.ShowMessage(heroLearn.Name + " Age: " + heroLearn.Age.ToString());
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, iPrice, false);
            GameMenu.SwitchToMenu("backstreet_matchmaker");

        }

        public static void DoMatchmakerButtonAgeAny(MenuCallbackArgs x, int iPrice)
        {

            MBList<Hero> listSpouse = Util.GetUnwedHeroesInKingdom(Settlement.CurrentSettlement.OwnerClan.Kingdom);

            Hero heroLearn = listSpouse.GetRandomElement();
            heroLearn.IsKnownToPlayer = true;

            listSpouse = null;
            Util.ShowMessage(heroLearn.Name + " Age: " + heroLearn.Age.ToString());
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, iPrice, false);
            GameMenu.SwitchToMenu("backstreet_matchmaker");

        }

        public static bool DoMatchmakerMenuCondition(MenuCallbackArgs x, GameMenuOption.LeaveType ltMenu)
        {

            x.optionLeaveType = ltMenu;
            if (Util.DoesKingdomHaveUnwedHeroes(Settlement.CurrentSettlement.OwnerClan.Kingdom))
            {
                //Util.ShowMessage("Kingdom has unwed heroes!");
                return true;
            }
            else
            {
                //Util.ShowMessage("No unwed heroes in kingdom!");
                return false;

            }

        }

        public static bool DoMatchmakerMenuButtonCondition(MenuCallbackArgs x, GameMenuOption.LeaveType ltMenu, int iPrice, int iMinAge, int iMaxAge)
        {

            x.optionLeaveType = GameMenuOption.LeaveType.Bribe;
            MBTextManager.SetTextVariable("PRICE", iPrice);
            if (Hero.MainHero.Gold > iPrice && Util.DoesKingdomHaveUnwedHeroesInAgeRange(Settlement.CurrentSettlement.OwnerClan.Kingdom, iMinAge, iMaxAge))
            {
                //Util.ShowMessage("Kingdom has unwed heroes!");
                return true;
            }
            else
            {
                //Util.ShowMessage("No unwed heroes in kingdom!");
                return false;

            }

        }

        public static void AddGameMenuMatchmaker(CampaignGameStarter cgsTeachSpouses)
        {

            Util.ShowMessage("AddGameMenuMatchmaker");

            cgsTeachSpouses.AddGameMenuOption(
                "town_backstreet",
                "backstreet_matchmaker",
                "{=AFyTyS17}Visit the matchmaker",
                (MenuCallbackArgs x) =>
                {

                    return DoMatchmakerMenuCondition(x, GameMenuOption.LeaveType.Submenu);
                    

                },
                delegate (MenuCallbackArgs x)
                {
                    GameMenu.SwitchToMenu("backstreet_matchmaker");
                }, false, 4, false);

            cgsTeachSpouses.AddGameMenu("backstreet_matchmaker",
                "{=AFyTyS18}The Matchmaker \n\nDeciding you need a helping hand, you came to a rather unsettling person, who loudly attests they are simply genuine in their love of helping others to find theirs. They have shared with you the rates for helping you along the way.",
                (MenuCallbackArgs x) => { },
                GameOverlays.MenuOverlayType.SettlementWithBoth,
                GameMenu.MenuFlags.None,
                null
                );

            cgsTeachSpouses.AddGameMenuOption(
                "backstreet_matchmaker",
                "backstreet_matchmaker_mmsixties",
                "{=AFyTyS24}A spouse older than 60: {PRICE}{GOLD_ICON}",
                (MenuCallbackArgs x) =>
                {
                    
                    return DoMatchmakerMenuButtonCondition(x, GameMenuOption.LeaveType.Bribe, tsSettings.MMfSixties, 60, 99);

                },
                delegate (MenuCallbackArgs x)
                {
                    DoMatchmakerButtonAgeRange(x, 60, 99, tsSettings.MMfSixties);
                }, false, -1, false);

            cgsTeachSpouses.AddGameMenuOption(
                "backstreet_matchmaker",
                "backstreet_matchmaker_mmFifties",
                "{=AFyTyS23}A spouse between 50 & 59: {PRICE}{GOLD_ICON}",
                (MenuCallbackArgs x) =>
                {

                    return DoMatchmakerMenuButtonCondition(x, GameMenuOption.LeaveType.Bribe, tsSettings.MMfFifties, 50, 59);

                },
                delegate (MenuCallbackArgs x)
                {
                    DoMatchmakerButtonAgeRange(x, 50, 59, tsSettings.MMfFifties);
                }, false, -1, false);

            cgsTeachSpouses.AddGameMenuOption(
                "backstreet_matchmaker",
                "backstreet_matchmaker_mmForties",
                "{=AFyTyS22}A spouse between 40 & 49: {PRICE}{GOLD_ICON}",
                (MenuCallbackArgs x) =>
                {

                    return DoMatchmakerMenuButtonCondition(x, GameMenuOption.LeaveType.Bribe, tsSettings.MMfForties, 40, 49);

                },
                delegate (MenuCallbackArgs x)
                {
                    DoMatchmakerButtonAgeRange(x, 40, 49, tsSettings.MMfForties);
                }, false, -1, false);

            cgsTeachSpouses.AddGameMenuOption(
                "backstreet_matchmaker",
                "backstreet_matchmaker_mmThirties",
                "{=AFyTyS21}A spouse between 30 & 39: {PRICE}{GOLD_ICON}",
                (MenuCallbackArgs x) =>
                {

                    return DoMatchmakerMenuButtonCondition(x, GameMenuOption.LeaveType.Bribe, tsSettings.MMfThirties, 30, 39);

                },
                delegate (MenuCallbackArgs x)
                {
                    DoMatchmakerButtonAgeRange(x, 30, 39, tsSettings.MMfThirties);
                }, false, -1, false);

            cgsTeachSpouses.AddGameMenuOption(
                "backstreet_matchmaker",
                "backstreet_matchmaker_mmTwenties",
                "{=AFyTyS20}A spouse between 20 & 29: {PRICE}{GOLD_ICON}",
                (MenuCallbackArgs x) =>
                {

                    return DoMatchmakerMenuButtonCondition(x, GameMenuOption.LeaveType.Bribe, tsSettings.MMfTwenties, 20, 29);

                },
                delegate (MenuCallbackArgs x)
                {
                    DoMatchmakerButtonAgeRange(x, 20, 29, tsSettings.MMfTwenties);
                }, false, -1, false);

            /*
            cgsTeachSpouses.AddGameMenuOption(
                "backstreet_matchmaker",
                "backstreet_matchmaker_mmAny",
                "{=AFyTyS19}A spouse of any age: {PRICE}{GOLD_ICON}",
                (MenuCallbackArgs x) =>
                {

                    return DoMatchmakerMenuButtonCondition(x, GameMenuOption.LeaveType.Bribe, tsSettings.MMfAny, 18, 99);

                },
                delegate (MenuCallbackArgs x)
                {

                    DoMatchmakerButtonAgeAny(x, tsSettings.MMfAny);

                }, false, -1, false);
            */


            //
            cgsTeachSpouses.AddGameMenuOption(
                "backstreet_matchmaker",
                "backstreet_matchmaker_leave",
                "{=AFyTyS25}Leave",
                (MenuCallbackArgs x) =>
                {

                    x.optionLeaveType = GameMenuOption.LeaveType.Leave;

                    return true;
                },
                delegate (MenuCallbackArgs x)
                {
                    GameMenu.SwitchToMenu("town");
                }, true, -1, false, null);


        }

        public static bool DoTeachMenuButtonCondition(MenuCallbackArgs x, GameMenuOption.LeaveType ltMenu)
        {

            if (Util.DoesPlayerHavePolygamy())
            {
                return true;
            }
            else if (Hero.MainHero.Spouse != null)
            {
                return true;
            }

            return false;


        }

        /*
        public static bool DoTeachMenuCondition(MenuCallbackArgs x, GameMenuOption.LeaveType ltMenu, bool bPolygamy, bool bDay)
        {
            int iTime = (int)CampaignTime.Now.CurrentHourInDay;
            bool bHasPolygamy = Util.DoesPlayerHavePolygamy();
            bool bSpouse = TaleWorlds.CampaignSystem.Hero.MainHero.Spouse != null;

            if (bDay && (iTime > 8 && iTime < 20))
            {
                if (bPolygamy && bHasPolygamy)
                    return true;
                else if (bHasPolygamy == false && bSpouse)
                    return true;
            }

            if (bDay == false && (iTime < 8 || iTime > 20))
            {
                if (bPolygamy && bHasPolygamy)
                    return true;
                else if (bPolygamy == false && bSpouse)
                    return true;
            }

            x.optionLeaveType = GameMenuOption.LeaveType.Submenu;

            return false;
        }
        */

        public static bool DoTeachMenuCondition(MenuCallbackArgs x, GameMenuOption.LeaveType ltMenu)
        {

            Hero heroPlayer = TaleWorlds.CampaignSystem.Hero.MainHero;
            bool bEnable = (Util.DoesPlayerHavePolygamy() || heroPlayer.Spouse != null);
            
            //todo
            

        }

        public static void AddGameMenuTeach(CampaignGameStarter cgsTeachSpouses)
        {

            Util.ShowMessage("AddGameMenuTeach");

            cgsTeachSpouses.AddGameMenu("arena_teach_spouse",
                "{=AFyTyS33}Teaching Grounds \n\nThe grounds outside of the arena have been kept tidy and cared for enough, that they look suitable enough to learn, or train, with your spouse.",
                (MenuCallbackArgs x) => {

                    DoTeachMenuCondition(x, GameMenuOption.LeaveType.Leave);
                    

                },
                GameOverlays.MenuOverlayType.SettlementWithBoth,
                GameMenu.MenuFlags.None,
                null
                );

            cgsTeachSpouses.AddGameMenuOption(
                "town_arena",
                "arena_teach",
                "{=AFyTyS01}Visit the teaching grounds",
                (MenuCallbackArgs x) =>
                {

                    return DoTeachMenuButtonCondition(x, GameMenuOption.LeaveType.Submenu);


                },
                delegate (MenuCallbackArgs x)
                {
                    GameMenu.SwitchToMenu("arena_teach");
                }, false, 4, false);

            //
            cgsTeachSpouses.AddGameMenuOption(
                "arena_teach",
                "arena_teach_leave",
                "{=AFyTyS25}Leave",
                (MenuCallbackArgs x) =>
                {

                    x.optionLeaveType = GameMenuOption.LeaveType.Leave;

                    return true;
                },
                delegate (MenuCallbackArgs x)
                {
                    GameMenu.SwitchToMenu("town_arena");
                }, true, -1, false, null);


        }
    }

    public class Util
    {

        public static bool bFoundSpousesExpanded;

        public static void ShowMessage(string strToShow)
        {

            InformationManager.DisplayMessage(new InformationMessage("TeachSpouses - " + strToShow));

        }

        public static void CreateClassroomData()
        {
        
            foreach (SkillObject skillCurrent in Skills.All)
            {
                
                tsTeachingSkill tsCurrent = new tsTeachingSkill();
                tsCurrent.Skill = skillCurrent;
                tsCurrent.Students = new List<tsCharacterToTeach>();

                Classroom.Instance.Teaching.Add(tsCurrent);

            }

        }

        public static CampaignBehaviorBase GetPlayerPolygamyBehavior()
        {
            IEnumerable<CampaignBehaviorBase> allCampaignBehaviors = Campaign.Current.GetCampaignBehaviors<CampaignBehaviorBase>();

            foreach (var behavior in allCampaignBehaviors)
            {
                if (behavior.GetType().Name == "PlayerPolygamyBehavior")
                    return behavior;
            }
            return null;
        }

        public static MBList<Hero> GetAllSpousesOfPlayer(bool bIncludeMonogamy = false)
        {
            FieldInfo spousesDataFieldInfo = AccessTools.Field("BannerlordExpanded.SpousesExpanded.Polygamy.Behaviors.PlayerPolygamyBehavior:_secondarySpouses");

            // We are getting all the spousesData of all the player heroes (including those who are deceased)
            Dictionary<Hero, MBList<Hero>> spousesData = (Dictionary<Hero, MBList<Hero>>)spousesDataFieldInfo.GetValue(Util.GetPlayerPolygamyBehavior());

            MBList<Hero> playerSpouses;
            if (spousesData.TryGetValue(Hero.MainHero, out playerSpouses)) // Using the current player hero as the key
            {
                // We found the spouses for the player
                return playerSpouses;
            }
            else if (Hero.MainHero.Spouse != null)
            {
                playerSpouses.Add(Hero.MainHero.Spouse);
            }
                // No spouses saved to the player
            return null;
        }

        public static bool DoesPlayerHavePolygamy()
        {
            MBList<Hero> listSpouses = GetAllSpousesOfPlayer();

            if (listSpouses != null && listSpouses.Count > 1)
                return true;
            else
                return false;
        }

        public static Hero GetUnwedHeroInSettlement(Hero heroOnTinder, Settlement settlementToSearch)
        {

            Hero heroSpouse = null;
            Hero heroCurrent;


            for (int iCounter = 0; iCounter < settlementToSearch.HeroesWithoutParty.Count - 1; iCounter++)
            {
                heroCurrent = settlementToSearch.HeroesWithoutParty[iCounter];

                if (Util.CanMarry(heroOnTinder, heroCurrent)
                )
                {
                    heroSpouse = heroCurrent;
                    break;
                }
            }

            return heroSpouse;

        }

        public static bool DoesKingdomHaveUnwedHeroes(Kingdom kingdomToCheck)
        {

            MBList<Hero> listHeroes = GetUnwedHeroesInKingdom(kingdomToCheck);
            FilterMarryListByKnown(listHeroes, true);

            if (listHeroes.Count > 0)
            {
                listHeroes = null;
                return true;
            }
            else
            {
                listHeroes = null;
                return false;
            }

        }
        public static bool DoesKingdomHaveUnwedHeroesInAgeRange(Kingdom kingdomToCheck, int ageMin, int ageMax)
        {

            MBList<Hero> listHeroes = GetUnwedHeroesInKingdom(kingdomToCheck);
            Util.FilterMarryListByAge(listHeroes, ageMin, ageMax);
            Util.FilterMarryListByKnown(listHeroes, true);

            if (listHeroes.Count > 0)
            {
                listHeroes = null;
                return true;
            }
            else
            {
                listHeroes = null;
                return false;
            }

        }

        public static MBList<Hero> GetUnwedHeroesInKingdom(Kingdom kingdomToSearch)
        {

            MBList<Hero> listSpouse = new MBList<Hero>();
            
            foreach (var heroFromKingdom in kingdomToSearch.Heroes)
            {
                if (Util.CanMarry(Hero.MainHero, heroFromKingdom))
                {
                    //Util.ShowMessage("GetUnwedHeroesInKingdom - Adding hero!");
                    listSpouse.Add(heroFromKingdom);
                }
            }

            //Util.ShowMessage("GetUnwedHeroes - listSpouse count: " + listSpouse.Count.ToString());

            Util.FilterMarryListByKnown(listSpouse, true);
            return listSpouse;

        }

        public static MBList<Hero> GetUnwedHeroesInKingdomFromSettlement(Hero heroOnTinder, Settlement settlementToSearch)
        {

            MBList<Hero> listSpouse = new MBList<Hero>();

            Util.ShowMessage("GetUnwedHeroes - Kingdom's hero count: " + settlementToSearch.OwnerClan.Kingdom.Heroes.Count.ToString());

            foreach (var heroFromKingdom in settlementToSearch.OwnerClan.Kingdom.Heroes)
            {
                if (Util.CanMarry(heroOnTinder, heroFromKingdom))
                {
                    Util.ShowMessage("GetUnwedHeroes - Adding hero!");
                    listSpouse.Add(heroFromKingdom);
                }
            }

            Util.ShowMessage("GetUnwedHeroes - listSpouse count: " + listSpouse.Count.ToString());

            return listSpouse;

        }

        public static bool CanMarry(Hero heroPlayer, Hero heroMarry)
        {
            
            return (heroMarry.IsActive
            && heroMarry.Spouse == null
            && heroMarry != heroPlayer
            && heroMarry.IsFemale != heroPlayer.IsFemale
            && heroMarry.IsLord
            && !heroMarry.IsClanLeader
            && !heroMarry.IsKingdomLeader
            && !heroMarry.IsMinorFactionHero
            && !heroMarry.IsNotable
            && !heroMarry.IsTemplate
            && heroMarry.Age >= 18);
            
        }

        public static bool FilterMarryListByAge(MBList<Hero> listMarry, int iMinAge, int iMaxAge)
        {

            for (int iCounter = listMarry.Count - 1; iCounter >= 0; iCounter--)
            {

                int iAge = (int)listMarry[iCounter].Age;

                if (iAge < iMinAge || iAge > iMaxAge)
                {

                    listMarry.RemoveAt(iCounter);

                }
                
            }

            if (listMarry.Count > 0)
                return true;
            else
                return false;

        }

        public static bool FilterMarryListByKnown(MBList<Hero> listMarry, bool bRemoveKnown)
        {

            for (int iCounter = listMarry.Count - 1; iCounter >= 0; iCounter--)
            {
                bool bKnown = listMarry[iCounter].IsKnownToPlayer;

                if (bRemoveKnown == bKnown)
                {

                    listMarry.RemoveAt(iCounter);

                }

            }

            if (listMarry.Count > 0)
            {
                return true;
            }

            return false;

        }

        public static CampaignBehaviorBase GetPolygamyMarriageOfferBehavior()
        {
            IEnumerable<CampaignBehaviorBase> allCampaignBehaviors = Campaign.Current.GetCampaignBehaviors<CampaignBehaviorBase>();

            foreach (var behavior in allCampaignBehaviors)

            {
                if (behavior.GetType().Name == "MarriageOfferForPlayerBehavior")
                    return behavior;
            }
            return null;
        }

        public static bool GetSkillsHigherThanSpouse(Hero heroSpouse, List<tsTeachingSkill> listResult)
        {

            List<tsCharacterToTeach> Result = new List<tsCharacterToTeach>();

            Hero heroPlayer = Hero.MainHero;
            int iSkillPlayer;
            int iSkillSpouse;

            bool bAddedAnything = false;

            foreach (tsTeachingSkill tsCurrent in Classroom.Instance.Teaching)
            {

                iSkillPlayer = heroPlayer.GetSkillValue(tsCurrent.Skill);
                iSkillSpouse = heroSpouse.GetSkillValue(tsCurrent.Skill);

                if (iSkillPlayer > iSkillSpouse)
                {
                    tsCharacterToTeach tsDataCurrent = new tsCharacterToTeach();
                    tsDataCurrent.Student = heroSpouse;
                    tsDataCurrent.NumLevelsCanBeTaught = iSkillPlayer - iSkillSpouse;
                    tsCurrent.Students.Add(tsDataCurrent);
                    bAddedAnything = true;
                }

            }

            return bAddedAnything;

        }

    }
    internal class sbTeachSpouses : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
        TaleWorlds.MountAndBlade.Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Message",
            new TextObject("Message", null),
            9990,
            () => { InformationManager.DisplayMessage(new InformationMessage("TeachSpouses - Hello World!")); },
            () => { return (false, null); }));
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            InformationManager.DisplayMessage(new InformationMessage("OnGameStart!"));
            
            if (game.GameType is Campaign)
            {
                CampaignGameStarter cgsTeachSpouses = (CampaignGameStarter)gameStarterObject;
                cgsTeachSpouses.AddBehavior(new sbTeachSpousesCampaign());

            }
        }

        


    }

    internal class sbTeachSpousesCampaign : CampaignBehaviorBase
    {

        public List<string> listText;


        #region Works
        public override void RegisterEvents()
        {

            //throw new NotImplementedException();

            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(CGS =>
            {
                TeachSpousesMenus.AddGameMenuMatchmaker(CGS);

            }));

            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(CGS =>
            {

                Util.ShowMessage("OnAfterSessionLaunched!");

                CampaignBehaviorBase cbbPolygamy = Util.GetPlayerPolygamyBehavior();

                if (cbbPolygamy != null)
                {

                    Util.ShowMessage("Found Spouses Expanded behaviour!");
                    Util.bFoundSpousesExpanded = true;

                }
                else
                {
                    Util.ShowMessage("Did not find Spouses Expanded behaviour!");
                }

            }));



            //CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(DoStuff) );

        }
        private void DoStuff(MobileParty mpEntered, Settlement smEntered, Hero hEntered)
        {

            if (hEntered != null && hEntered.IsHumanPlayerCharacter)
            {
                Util.ShowMessage("AfterSettlementEntered!");

                if (hEntered.Spouse == null && Util.bFoundSpousesExpanded == false)
                {
                    Util.ShowMessage("Player has no spouse!");

                    Hero heroFound = Util.GetUnwedHeroInSettlement(hEntered, smEntered);
                    if (heroFound != null)
                    {

                        //Works!
                        //TaleWorlds.CampaignSystem.Actions.MarriageAction.Apply(hEntered, heroFound, true);

                        Util.ShowMessage("Valid spouse found!");
                        MarriageOfferCampaignBehavior bhvMarriage = Campaign.Current.GetCampaignBehavior<MarriageOfferCampaignBehavior>();

                        bhvMarriage.CreateMarriageOffer(hEntered, heroFound);
                    }
                    else
                    {
                        Util.ShowMessage("No valid spouse found!");
                    }
                     

                }
                else if (Util.bFoundSpousesExpanded)
                {
                    Util.ShowMessage("Player can get all the spouses!");

                    //Works, but you can only have 1 offer at a time without bugs
                    /*
                    MarriageOfferCampaignBehavior bhvMarriage = Campaign.Current.GetCampaignBehavior<MarriageOfferCampaignBehavior>();
                    Hero heroToMarry = GetUnwedHeroInSettlementFromMultiple(hEntered, smEntered);
                    if (heroToMarry != null)
                    {
                        bhvMarriage.CreateMarriageOffer(hEntered, heroToMarry); 
                    }
                    */
                    


                    MBList<Hero> listHeroesFound = Util.GetUnwedHeroesInKingdomFromSettlement(hEntered, smEntered);
                    Util.ShowMessage(listHeroesFound.Count.ToString());

                    for (var iCounter = 0; iCounter < listHeroesFound.Count; iCounter++)
                    {
                        Hero heroCurrent = listHeroesFound[iCounter];
                        Util.ShowMessage(heroCurrent.StringId);

                        Util.ShowMessage("Value for player's faction: " + Campaign.Current.Models.DiplomacyModel.GetValueOfHeroForFaction(heroCurrent, hEntered.MapFaction, true).ToString());

                        //In order to do multiple marriages at once, we need to use marriageaction
                        //Otherwise there is a bug with issuing multiple marriage offers where they all get corrupted with the wrong npc
                        TaleWorlds.CampaignSystem.Actions.MarriageAction.Apply(hEntered, heroCurrent);

                        
                    }
                    

                }

            }

        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }

        #endregion
        

        private Hero GetUnwedHeroInSettlementFromMultiple(Hero heroOnTinder, Settlement settlementToSearch)
        {

            List<Hero> listHeroes = new List<Hero>();
            Hero heroCurrent = null;


            for (int iCounter = 0; iCounter < settlementToSearch.HeroesWithoutParty.Count - 1; iCounter++)
            {
                heroCurrent = settlementToSearch.HeroesWithoutParty[iCounter];

                if ( Util.CanMarry(heroOnTinder, heroCurrent) )
                {
                    listHeroes.Add(heroCurrent);
                }
            }

            if (listHeroes.Count > 0)
            {
                heroCurrent = listHeroes.GetRandomElement<Hero>();
            }
            
            listHeroes = null;
            return heroCurrent;

        }


    }


}
