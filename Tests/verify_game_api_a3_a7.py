#!/usr/bin/env python3
"""Stage A3-A7 *source-contract* verification; never claims native build or runtime success.

Checks each new bridge against exact source-confirmed game declarations. Native
install and in-game behavior are independently tested by Tests/NativeGameApi.
Usage: python Tests/verify_game_api_a3_a7.py --game-source ../Source
"""
from __future__ import annotations
from pathlib import Path
import argparse, hashlib, json, re, sys

parser=argparse.ArgumentParser()
parser.add_argument('--game-source',type=Path,default=None)
parser.add_argument('--report',type=Path,default=None)
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
fn=lambda s:(root/s).read_text(encoding='utf-8-sig')
checks=[]
def check(stage,label,ok,evidence=None,skip=False):
    checks.append({'stage':stage,'name':label,'result':'SKIP' if skip else 'PASS' if ok else 'FAIL','evidence':evidence})

interfaces={
 'A3':{
  'IGameItemsService':'GameItemsService',
  'IGameEconomyService':'GameEconomyService',
  'IGameProductionService':'GameProductionService',
 },
 'A4':{
  'IGameAdventureService':'GameAdventureService',
  'IGameCombatService':'GameCombatService',
  'IGameProgressService':'GameProgressService',
 },
 'A5':{'IGameWorldService':'GameWorldService','IWorkspaceEditorService':'WorkspaceEditorService'},
 'A6':{'IGameUiService':'GameUiService','IGameViewService':'GameViewService','IGamePresentationService':'GamePresentationService'},
}
service_path={
 'GameItemsService':'Gameplay','GameEconomyService':'Gameplay','GameProductionService':'Gameplay',
 'GameAdventureService':'Gameplay','GameCombatService':'Gameplay','GameProgressService':'Gameplay',
 'GameWorldService':'Gameplay','WorkspaceEditorService':'Workspace',
 'GameUiService':'Gameplay','GameViewService':'Gameplay','GamePresentationService':'Gameplay',
}
new_members={
 'IGameItemsService': ['Enhancements','FindEnhancement','EquipmentEnhancements','EquipmentStat','IsUnlocked','NativeBaseValue','NativeSellValue','AppraiserStat','CanEnhance','RecipeAvailable','RecipeCost','RecipeReward','RecipeReagents','RecipeResults','RequiredQuantity','ProducedQuantity','NativeItemSources'],
 'IGameEconomyService':['CanAfford','ResourceCostMultiplier','AdditionalWorkspaceCost'],
 'IGameProductionService':['TotalForestWorkers','ForestBlocked','FarmBlocked','ForestUtilization','ForestQuotaMultiplier','ForestResourceUnlocked','FarmResourceUnlocked','NextForestWorkerCost','FarmExpansionCost'],
 'IGameAdventureService':['QuestDuration','QuestRewards','QuestUnlocked','IsDungeonAvailable','DungeonSupplyCost','DungeonStages','HasWarStage','WarCurrentStage'],
 'IGameCombatService':['CombatPower','BaseDamage','DamageReduction','HasTrait','Traits','CanEquip','ConflictingEquipment','Equipped'],
 'IGameProgressService':['Requirements','AvailableChildren','ResearchAvailable','ResearchLocked','ResearchCost','ResearchCostTier'],
 'IGameWorldService':['ActiveFlowIndex','NativeMachineCount','NativePipeCount','NativeDecorationCount','ElementsIn'],
 'IWorkspaceEditorService':['MoveToFlow','MoveDecorationToFlow'],
 'IGameUiService':['NativeUi'],
 'IGameViewService':['NativeInput','NativeCamera'],
 'IGamePresentationService':['NativeLocales','TranslateOnly'],
}
for stage,pairs in interfaces.items():
 for intf,impl in pairs.items():
  a=fn('src/Foundation/Api/'+intf+'.cs')
  b=fn('src/Foundation/'+service_path[impl]+'/'+impl+'.cs')
  for member in new_members[intf]:
   ia=bool(re.search(r'\b'+re.escape(member)+r'\b',a))
   ib=bool(re.search(r'\b'+re.escape(member)+r'\b',b))
   check(stage,intf+'.'+member,ia and ib,service_path[impl]+'/'+impl+'.cs')

# Required stage boundaries; don't accidentally rewrite Foundation-owned services.
bootstrap=fn('src/Foundation/Bootstrap/FoundationPlugin.FoundationPlugin.cs')
csproj=fn('Foundation.csproj')
project_version=re.search(r'<Version>([0-9.]+)</Version>',csproj).group(1)
check('Boundary','One production Foundation.dll','<AssemblyName>Foundation</AssemblyName>' in csproj and '<Version>'+project_version+'</Version>' in csproj)
check('Boundary','No second transport or core registry',all('new FlowPipe()' not in fn('src/Foundation/'+service_path[impl]+'/'+impl+'.cs') for pairs in interfaces.values() for impl in pairs.values()))
check('Boundary','No signal rewrite',not any('SignalRegistry' in member for methods in new_members.values() for member in methods))
check('Boundary','Native Event Integration boots from unified plugin','GameEventIntegration.Install(hooks)' in bootstrap)
check('A3','C# 14 ItemEnhancement registry getter avoids reserved field keyword',
      'var enhancementRegistryField = AccessTools.Field(typeof(ItemEnhancement), "_allEnhancements")' in fn('src/Foundation/Gameplay/GameItemsService.cs') and
      'enhancementRegistryField.GetValue(null)' in fn('src/Foundation/Gameplay/GameItemsService.cs') and
      not re.search(r'\bvar\s+field\b', fn('src/Foundation/Gameplay/GameItemsService.cs')),
      'C# 14 CS9273/CS9258; get actual game private ItemEnhancement._allEnhancements via Harmony AccessTools.Field')
check('A3','Native resource-price multiplier preserves double precision',
      'double ResourceCostMultiplier(ResourceItemType resource)' in fn('src/Foundation/Api/IGameEconomyService.cs') and
      'double ResourceCostMultiplier(ResourceItemType resource)' in fn('src/Foundation/Gameplay/GameEconomyService.cs'),
      'Assets.Source.Player/GamePlayer.cs: GetResourceCostMultiplier returns double')

hooks={
 'NativeWorkspaceSelected':'WorldFlowManager.ShowFlow',
 'NativePopupVisibility':'GameUI.TogglePopupMenu',
 'NativeEquipmentAdded':'HumanItemData.AddEquipment',
 'NativeEquipmentRemoved':'HumanItemData.RemoveEquipment',
 'NativeDamage':'CombatantItemData.TakeDamage',
 'NativeHeal':'CombatantItemData.Heal',
 'NativeEnergy':'CombatantItemData.LoseEnergy',
}
install=fn('src/Foundation/Gameplay/GameEventIntegration.cs')
for name,origin in hooks.items():
 check('A7','Hook registered: '+name,'typeof('+name+')' in install,origin)
file_origins={
 'Gameplay/Nested/NativeWorkspaceSelected.cs':('WorldFlowManager','ShowFlow'),
 'Gameplay/Nested/NativePopupVisibility.cs':('GameUI','TogglePopupMenu'),
 'Gameplay/Nested/NativeEquipmentAdded.cs':('HumanItemData','AddEquipment'),
 'Gameplay/Nested/NativeEquipmentRemoved.cs':('HumanItemData','RemoveEquipment'),
 'Gameplay/Nested/NativeCombatantVitals.cs':('CombatantItemData','TakeDamage'),
}
for file,(type_name,method) in file_origins.items():
 body=fn('src/Foundation/'+file)
 check('A7','Native Harmony origin '+type_name+'.'+method, f'typeof({type_name})' in body and f'nameof({type_name}.{method})' in body,file)
for evt in ('NativeEquipmentChangedEvent','NativeWorkspaceSelectedEvent','NativePopupVisibilityEvent','NativeCombatantVitalsChangedEvent'):
 check('A7','Post-commit event: '+evt,'sealed class '+evt in fn('src/Foundation/Api/'+evt+'.cs') and 'Publish(new '+evt in ''.join(fn('src/Foundation/Gameplay/Nested/'+n+'.cs') for n in ('NativeEquipmentAdded','NativeEquipmentRemoved','NativeWorkspaceSelected','NativePopupVisibility','NativeCombatantVitals')))

suite=fn('Tests/NativeGameApi/NativeGameApiVerificationPlugin.cs')
ps1=fn('Tests/NativeGameApi/Invoke-NativeGameApiVerification.ps1')
check('Verification','Independent BepInEx plugin, not production','[BepInPlugin(' in suite and 'Foundation.NativeGameApiVerification' in fn('Tests/NativeGameApi/Foundation.NativeGameApiVerification.csproj'))
check('Verification','Runtime PASS/FAIL/PENDING truthful','"PENDING"' in suite and '"FAIL"' in suite and '"PASS"' in suite)
check('Verification','Disposable mutations opt-in only','-foundation-gameapi-mutate' in suite and 'if (allowMutation && !mutated)' in suite)
check('Verification','Installed binary hashing + stale report guard',all(x in ps1 for x in ('Get-FileHash','LastWriteTimeUtc','Stale report','ConvertFrom-Json')))
check('Verification','One-step A3-A7 full build install',all(x in fn('Tests/NativeGameApi/INSTALL-A3-A7-TEST.cmd') for x in ('Install-Foundation.ps1','BuildInstall')))
check('Verification','Farm expansion compares actual native currency amounts',
      'FarmExpansionCost(1).GetCurrency().SequenceEqual(' in suite and 'GetPlotCost(1).GetCurrency()' in suite)
check('Verification','No-op move verifies native graph remains unchanged',
      'player.ActiveFlow.Elements.Count == count' in suite and 'ReferenceEquals(existingParent, element.ParentFlow)' in suite)
check('Verification','Reversible move remains pending until an eligible machine is present',
      'results["A5.ReversibleCrossFlowMove"].Status != "PENDING"' in suite)


signature_map={
 'A3':{
  'Assets.Source.Items/ItemType.cs':['public virtual long BaseValue','public virtual bool IsUnlocked','public List<string> GetItemSources'],
  'Assets.Source.Items/InventoryItem.cs':['public bool CanEnhance(EnhancementType type)','public int GetAppraiserStat(AppraiserStat stat)'],
  'Assets.Source.Items/CraftingRecipe.cs':['public bool IsAvailable','public int GetReagentCount(ItemType item)','public int GetResultCount(ItemType item)'],
  'Assets.Source.Items/ItemEnhancement.cs':['private static Dictionary<string, ItemEnhancement> _allEnhancements'],
  'Assets.Source.Items.Data/EquipmentItemData.cs':['public IEnumerable<ItemEnhancementData> Enhancements','public int GetStat(EquipStat s)'],
  'Assets.Source.Player/GamePlayer.cs':['public bool CanAfford(Currency c, long amt)','public float GetExchangeTax(Currency currency)','public double GetResourceCostMultiplier(ResourceItemType resource)','public CurrencyCollection GetExtraWorkflowCost'],
  'Assets.Source.Player/ForestSettings.cs':['public int TotalWorkers','public float GetUtilization(ForestWorker workerType)','public bool AddWorkerCount(ForestWorker type, int add)'],
  'Assets.Source.Player/FarmSettings.cs':['public CurrencyCollection GetPlotCost(int newPlots)'],
 },
 'A4':{
  'Assets.Source.Adventure/Quest.cs':['public int TickDuration','public IReadOnlyList<QuestReward> GetRewards(int completedCount)'],
  'Assets.Source.Adventure/DungeonType.cs':['public bool IsAvailableFor(GamePlayer current)'],
  'Assets.Source.Player/TechNode.cs':['public IEnumerable<TechNode> AllRequirements','public CurrencyCollection GetCost()'],
  'Assets.Source.Items.Data/CombatantItemData.cs':['public virtual int GetCombatPower()','public float GetDamageReduction(AdventurerTrait resist)','public bool HasTrait(AdventurerTrait trait)'],
  'Assets.Source.Items.Data/HumanItemData.cs':['public bool CanEquipItem(InventoryItem item, bool replace)','public List<InventoryItem> GetConflictingItems(InventoryItem item)'],
 },
 'A5':{
  'Assets.Source.Flow/FlowDiagram.cs':['public readonly List<FlowElement> Elements','public readonly List<FlowPipe> Pipes','public readonly List<FlowDeco> Decorations'],
  'Assets.Behaviour.Flow/WorldFlowManager.cs':['public void MoveToFlow(WorldFlowElement element, int moveFrom, int moveTo)','public void MoveToFlow(WorldFlowDeco deco, int moveFrom, int moveTo, bool temporary = false)'],
 },
 'A6':{
  'Assets.Behaviour.UI/GameUI.cs':['public static GameUI Instance','public void TogglePopupMenu(UIPopupMenu popup, bool toggleTo, bool playSound = true)'],
  'Assets.Behaviour.Input/PlayerInput.cs':['public static InputActionAsset Asset'],
  'Assets.Source.Util/Translation.cs':['public static IEnumerable<Translation> All','public static string TranslateOnly(string text, params object[] values)'],
 },
 'A7':{
  'Assets.Source.Items.Data/CombatantItemData.cs':['public bool TakeDamage(float dmg, object context, bool ignoreArmor = false)','public void Heal(float amt, object context)','public bool LoseEnergy(float amt, object context)'],
  'Assets.Source.Items.Data/HumanItemData.cs':['public void AddEquipment(InventoryItem item)','public void RemoveEquipment(EquipmentSlot slot)'],
  'Assets.Behaviour.Flow/WorldFlowManager.cs':['public void ShowFlow(int idx)'],
 },
}
if args.game_source and args.game_source.is_dir():
 for stage,files in signature_map.items():
  for name,needles in files.items():
   p=args.game_source/name
   if not p.exists():
    check(stage,'Native source file exists: '+name,False)
    continue
   contents=p.read_text(encoding='utf-8-sig')
   for needle in needles:
    check(stage,'Source: '+name+': '+needle,needle in contents,name)
else:
 check('Source','Actual game source signature verification',False,'Pass --game-source <extracted KingdomIncSrc/Source>',skip=True)

summary={v:sum(c['result']==v for c in checks) for v in ('PASS','FAIL','SKIP')}
report={'name':'Foundation A3-A7 Native Game API source contracts','version':project_version,'originalMilestone':'A3-A7 0.10.6','sourceOnly':True,
  'windowsCompiled':False,'unityRuntimeVerified':False,'summary':summary,'checks':checks}
if args.report:
 args.report.parent.mkdir(parents=True,exist_ok=True)
 args.report.write_text(json.dumps(report,indent=2),encoding='utf-8')
print(f"A3-A7 source contracts: PASS={summary['PASS']} FAIL={summary['FAIL']} SKIP={summary['SKIP']}")
for c in checks:
 if c['result']!='PASS':print(c['result'],c['name'],c['evidence'])
sys.exit(1 if summary['FAIL'] else 0)
