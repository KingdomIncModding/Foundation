using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using Assets.Source.Util;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomFoundation
{
    internal sealed class CheatCmd
    {
        const int WindowId = 765143;
        const string InputControlName = "FoundationCheatCmdInput";
        readonly List<string> log = new List<string>();
        readonly List<string> history = new List<string>();
        Rect windowRect = new Rect(60, 60, 680, 460);
        Vector2 scrollPosition;
        string inputText = "";
        int historyIndex = -1;
        bool isOpen;
        bool focusInput;
        bool restoreGameInput;

        internal bool IsOpen => isOpen;

        internal void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.ctrlKey.isPressed && keyboard.shiftKey.isPressed && !keyboard.altKey.isPressed && keyboard.pageDownKey.wasPressedThisFrame)
            {
                Toggle();
                return;
            }

            if (!isOpen)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        internal void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        internal void Open()
        {
            if (isOpen)
                return;
            isOpen = true;
            focusInput = true;
            restoreGameInput = Assets.Behaviour.Input.PlayerInput.Enabled;
            if (Assets.Behaviour.Input.PlayerInput.Instance)
                Assets.Behaviour.Input.PlayerInput.Disable();
            if (log.Count == 0)
            {
                AppendLog("Foundation CheatCmd ready. Type 'help' for commands (Ctrl+Shift+PageDn or Esc to close).");
            }
        }

        internal void Close()
        {
            if (!isOpen)
                return;
            isOpen = false;
            if (restoreGameInput && Assets.Behaviour.Input.PlayerInput.Instance && FoundationApi.UiAccess.Root && !FoundationApi.UiAccess.IsMenuVisible)
                Assets.Behaviour.Input.PlayerInput.Enable();
            restoreGameInput = false;
        }

        internal void Shutdown()
        {
            Close();
        }

        internal void OnGUI()
        {
            if (!isOpen)
                return;
            windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "Foundation CheatCmd [Ctrl+Shift+PageDn]");
        }

        void DrawWindow(int id)
        {
            HandleKeyboardEvents();
            DrawQuickBar();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUI.skin.box, GUILayout.ExpandHeight(true));
            for (int i = 0; i < log.Count; i++)
                GUILayout.Label(log[i]);
            GUILayout.EndScrollView();
            DrawInputRow();
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 22));
        }

        void DrawQuickBar()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+100 Gold", GUILayout.Width(86)))
                ExecuteCommand("gold 100");
            if (GUILayout.Button("+50 Silver", GUILayout.Width(86)))
                ExecuteCommand("silver 50");
            if (GUILayout.Button("+All Currencies", GUILayout.Width(114)))
                ExecuteCommand("currency all 1000");
            if (GUILayout.Button("Unlock Mods", GUILayout.Width(96)))
                ExecuteCommand("unlock mods");
            if (GUILayout.Button("Relock Mods", GUILayout.Width(96)))
                ExecuteCommand("relock mods");
            if (GUILayout.Button("Help", GUILayout.Width(54)))
                ExecuteCommand("help");
            if (GUILayout.Button("Clear", GUILayout.Width(54)))
                ExecuteCommand("clear");
            if (GUILayout.Button("X", GUILayout.Width(28)))
                Close();
            GUILayout.EndHorizontal();
        }

        void DrawInputRow()
        {
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName(InputControlName);
            inputText = GUILayout.TextField(inputText ?? "", GUILayout.ExpandWidth(true));
            if (focusInput)
            {
                GUI.FocusControl(InputControlName);
                focusInput = false;
            }
            if (GUILayout.Button("Run", GUILayout.Width(68)))
                SubmitCurrentInput();
            GUILayout.EndHorizontal();
        }

        void HandleKeyboardEvents()
        {
            var current = Event.current;
            if (current == null || current.type != EventType.KeyDown)
                return;

            if (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
            {
                current.Use();
                SubmitCurrentInput();
            }
            else if (current.keyCode == KeyCode.Escape)
            {
                current.Use();
                Close();
            }
            else if (current.keyCode == KeyCode.UpArrow && history.Count > 0)
            {
                current.Use();
                historyIndex = historyIndex <= 0 ? 0 : historyIndex - 1;
                inputText = history[historyIndex];
                focusInput = true;
            }
            else if (current.keyCode == KeyCode.DownArrow && history.Count > 0)
            {
                current.Use();
                if (historyIndex < history.Count - 1)
                {
                    historyIndex++;
                    inputText = history[historyIndex];
                }
                else
                {
                    historyIndex = history.Count;
                    inputText = "";
                }
                focusInput = true;
            }
        }

        void SubmitCurrentInput()
        {
            string command = (inputText ?? "").Trim();
            inputText = "";
            focusInput = true;
            if (command.Length == 0)
                return;
            if (history.Count == 0 || !string.Equals(history[history.Count - 1], command, StringComparison.Ordinal))
                history.Add(command);
            historyIndex = history.Count;
            ExecuteCommand(command);
        }

        internal void ExecuteCommand(string rawCommand)
        {
            string trimmed = (rawCommand ?? "").Trim();
            if (trimmed.Length == 0)
                return;
            AppendLog("> " + trimmed);
            try
            {
                var tokens = SplitTokens(trimmed);
                if (tokens.Count == 0)
                    return;
                string verb = tokens[0].ToLowerInvariant();
                switch (verb)
                {
                    case "help":
                    case "?":
                        PrintHelp();
                        break;
                    case "clear":
                    case "cls":
                        log.Clear();
                        break;
                    case "unlock":
                        HandleTechUnlock(tokens.Skip(1).ToList(), recursive: false);
                        break;
                    case "unlock+":
                        HandleTechUnlock(tokens.Skip(1).ToList(), recursive: true);
                        break;
                    case "lock":
                    case "relock":
                        HandleTechLock(tokens.Skip(1).ToList());
                        break;
                    case "tech":
                        HandleTechSubcommand(tokens.Skip(1).ToList());
                        break;
                    case "gold":
                        HandleCoinShortcut(tokens.Skip(1).ToList(), 10000L, "gold");
                        break;
                    case "silver":
                        HandleCoinShortcut(tokens.Skip(1).ToList(), 100L, "silver");
                        break;
                    case "copper":
                        HandleCoinShortcut(tokens.Skip(1).ToList(), 1L, "copper");
                        break;
                    case "currency":
                    case "money":
                    case "addcurrency":
                        HandleCurrencyCommand(tokens.Skip(1).ToList());
                        break;
                    case "resource":
                    case "res":
                    case "resources":
                        HandleResourceCommand(tokens.Skip(1).ToList());
                        break;
                    case "nightmare":
                    case "prestige":
                        HandleNightmareCommand(tokens.Skip(1).ToList());
                        break;
                    case "speed":
                    case "timescale":
                        HandleSpeedCommand(tokens.Skip(1).ToList());
                        break;
                    default:
                        AppendLog("Unknown command '" + tokens[0] + "'. Type 'help' for a list of commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                AppendLog("Error: " + ex.Message);
            }
        }

        void PrintHelp()
        {
            AppendLog("Commands:");
            AppendLog("  unlock <id|name|all|mods|native>   - Unlock a tech node (or all/mods/native)");
            AppendLog("  unlock+ <id|name>                  - Unlock a tech node and its prerequisites");
            AppendLog("  relock <id|name|all|mods|native>   - Relock a tech node (alias: lock)");
            AppendLog("  tech list [filter]                 - List tech nodes matching optional filter");
            AppendLog("  gold <amount>                      - Add displayed Gold coins (e.g. gold 100)");
            AppendLog("  silver <amount>                    - Add displayed Silver coins (e.g. silver 25)");
            AppendLog("  copper <amount>                    - Add displayed Copper coins");
            AppendLog("  currency <type|all> <amount>       - Add currency (Gold supports 100g/15s/50c)");
            AppendLog("  resource <id|name|all> <amount>    - Add resource quota/sec (e.g. res iron_ore 10)");
            AppendLog("  resource list [filter]             - List resource items and current quota");
            AppendLog("  nightmare <points>                 - Add Nightmare/Prestige points");
            AppendLog("  speed <multiplier>                 - Set game speed (e.g. speed 1, speed 4)");
            AppendLog("  clear                              - Clear console log");
        }

        void HandleTechSubcommand(List<string> args)
        {
            if (args.Count == 0)
            {
                ListTech(null);
                return;
            }
            string sub = args[0].ToLowerInvariant();
            if (sub == "unlock")
                HandleTechUnlock(args.Skip(1).ToList(), recursive: false);
            else if (sub == "unlock+")
                HandleTechUnlock(args.Skip(1).ToList(), recursive: true);
            else if (sub == "lock" || sub == "relock")
                HandleTechLock(args.Skip(1).ToList());
            else if (sub == "list")
                ListTech(args.Count > 1 ? string.Join(" ", args.Skip(1)) : null);
            else
                ListTech(string.Join(" ", args));
        }

        void HandleTechUnlock(List<string> args, bool recursive)
        {
            RequireKingdom();
            if (args.Count == 0)
            {
                AppendLog("Usage: unlock <id|name|all|mods|native>");
                return;
            }
            string target = string.Join(" ", args).Trim();
            if (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase))
            {
                int count = 0;
                foreach (var node in TechNode.Nodes)
                {
                    if (!node.DevMode && FoundationApi.Progression.UnlockTech(node.Identifier, false))
                        count++;
                }
                AppendLog("Unlocked " + count + " technology node(s).");
                return;
            }
            if (string.Equals(target, "mods", StringComparison.OrdinalIgnoreCase))
            {
                int count = 0;
                foreach (var node in FoundationApi.TechNodes.Nodes)
                {
                    if (FoundationApi.Progression.UnlockTech(node.Id, false))
                        count++;
                }
                AppendLog("Unlocked " + count + " mod technology node(s).");
                return;
            }
            if (string.Equals(target, "native", StringComparison.OrdinalIgnoreCase))
            {
                int count = 0;
                foreach (var node in TechNode.Nodes)
                {
                    if (!node.DevMode && !NativeResearch.Owns(node) && FoundationApi.Progression.UnlockTech(node.Identifier, false))
                        count++;
                }
                AppendLog("Unlocked " + count + " native technology node(s).");
                return;
            }

            var resolved = ResolveTechNode(target);
            if (resolved == null)
            {
                AppendLog("No tech node found matching '" + target + "'. Try 'tech list " + target + "'.");
                return;
            }

            if (recursive)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal);
                int count = UnlockWithPrerequisites(resolved, visited);
                AppendLog("Unlocked '" + TranslateName(resolved.Name) + "' (" + resolved.Identifier + ") and prerequisites (" + count + " newly unlocked).");
                return;
            }

            bool changed = FoundationApi.Progression.UnlockTech(resolved.Identifier, true);
            AppendLog(changed
                ? "Unlocked '" + TranslateName(resolved.Name) + "' (" + resolved.Identifier + ")."
                : "'" + TranslateName(resolved.Name) + "' (" + resolved.Identifier + ") is already unlocked.");
        }

        int UnlockWithPrerequisites(TechNode node, HashSet<string> visited)
        {
            if (node == null || !visited.Add(node.Identifier))
                return 0;
            int count = 0;
            if (node.Root != null && node.Root != node)
                count += UnlockWithPrerequisites(node.Root, visited);
            foreach (var req in node.AllRequirements)
                count += UnlockWithPrerequisites(req, visited);
            if (!node.DevMode && FoundationApi.Progression.UnlockTech(node.Identifier, false))
                count++;
            return count;
        }

        void HandleTechLock(List<string> args)
        {
            RequireKingdom();
            if (args.Count == 0)
            {
                AppendLog("Usage: relock <id|name|all|mods|native>");
                return;
            }
            string target = string.Join(" ", args).Trim();
            if (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase))
            {
                int count = 0;
                foreach (var node in TechNode.Nodes)
                {
                    if (node.Identifier == "mine" || node.Identifier == "forge" || node.Identifier == "iron_ingot")
                        continue;
                    if (FoundationApi.Progression.LockTech(node.Identifier))
                        count++;
                }
                AppendLog("Relocked " + count + " technology node(s) (kept starting mine/forge/iron_ingot).");
                return;
            }
            if (string.Equals(target, "mods", StringComparison.OrdinalIgnoreCase))
            {
                int count = 0;
                foreach (var node in FoundationApi.TechNodes.Nodes)
                {
                    if (FoundationApi.Progression.LockTech(node.Id))
                        count++;
                }
                AppendLog("Relocked " + count + " mod technology node(s).");
                return;
            }
            if (string.Equals(target, "native", StringComparison.OrdinalIgnoreCase))
            {
                int count = 0;
                foreach (var node in TechNode.Nodes)
                {
                    if (NativeResearch.Owns(node) || node.Identifier == "mine" || node.Identifier == "forge" || node.Identifier == "iron_ingot")
                        continue;
                    if (FoundationApi.Progression.LockTech(node.Identifier))
                        count++;
                }
                AppendLog("Relocked " + count + " native technology node(s).");
                return;
            }

            var resolved = ResolveTechNode(target);
            if (resolved == null)
            {
                AppendLog("No tech node found matching '" + target + "'. Try 'tech list " + target + "'.");
                return;
            }

            bool changed = FoundationApi.Progression.LockTech(resolved.Identifier);
            AppendLog(changed
                ? "Relocked '" + TranslateName(resolved.Name) + "' (" + resolved.Identifier + ")."
                : "'" + TranslateName(resolved.Name) + "' (" + resolved.Identifier + ") was already locked.");
        }

        void ListTech(string filter)
        {
            var player = GamePlayer.Current;
            var matches = TechNode.Nodes
                .Where(n => !n.DevMode)
                .Where(n => string.IsNullOrEmpty(filter)
                    || n.Identifier.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                    || TranslateName(n.Name).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                    || (NativeResearch.ModOf(n)?.Name?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                .Take(40)
                .ToList();

            if (matches.Count == 0)
            {
                AppendLog("No tech nodes matched '" + filter + "'.");
                return;
            }

            foreach (var node in matches)
            {
                bool unlocked = player != null && player.HasTech(node);
                var mod = NativeResearch.ModOf(node);
                string tag = mod != null ? "[Mod: " + mod.Name + "]" : "[Native]";
                AppendLog("  " + (unlocked ? "[UNLOCKED] " : "[LOCKED]   ") + node.Identifier + " — " + TranslateName(node.Name) + " " + tag);
            }
        }

        void HandleCoinShortcut(List<string> args, long multiplier, string unitName)
        {
            var player = RequireKingdom();
            if (args.Count == 0 || !double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                AppendLog("Usage: " + unitName + " <amount>");
                return;
            }
            long rawDelta = (long)Math.Round(value * multiplier);
            player.AddCurrency(Currency.Gold, rawDelta, false);
            AppendLog("Added " + value.ToString(CultureInfo.InvariantCulture) + " " + unitName + " (" + FormatGold(player.GetCurrency(Currency.Gold)) + " total).");
        }

        void HandleCurrencyCommand(List<string> args)
        {
            var player = RequireKingdom();
            if (args.Count < 2)
            {
                AppendLog("Usage: currency <Gold|Requisition|Medal|Ring|Mana|Crown|Ducat|Ingenuity|Faith|all> <amount>");
                return;
            }
            string target = args[0];
            string amountText = args[1];
            if (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (!long.TryParse(amountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long amount))
                {
                    AppendLog("Invalid integer amount '" + amountText + "'.");
                    return;
                }
                foreach (Currency currency in Enum.GetValues(typeof(Currency)))
                {
                    long scaled = currency == Currency.Gold ? amount * 10000L : amount;
                    player.AddCurrency(currency, scaled, false);
                }
                AppendLog("Added " + amount + " to all currencies (Gold: +" + amount + "g).");
                return;
            }

            if (!Enum.TryParse<Currency>(target, true, out var resolved) || !Enum.IsDefined(typeof(Currency), resolved))
            {
                AppendLog("Unknown currency '" + target + "'. Valid: " + string.Join(", ", Enum.GetNames(typeof(Currency))));
                return;
            }

            long delta = ParseCurrencyAmount(resolved, amountText);
            player.AddCurrency(resolved, delta, false);
            long total = player.GetCurrency(resolved);
            AppendLog("Updated " + resolved + ": " + (resolved == Currency.Gold ? FormatGold(total) : total.ToString("N0", CultureInfo.InvariantCulture)) + ".");
        }

        static long ParseCurrencyAmount(Currency currency, string text)
        {
            string s = (text ?? "").Trim();
            if (currency == Currency.Gold)
            {
                if (s.EndsWith("g", StringComparison.OrdinalIgnoreCase) && double.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double g))
                    return (long)Math.Round(g * 10000.0);
                if (s.EndsWith("s", StringComparison.OrdinalIgnoreCase) && double.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double sv))
                    return (long)Math.Round(sv * 100.0);
                if (s.EndsWith("c", StringComparison.OrdinalIgnoreCase) && long.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out long c))
                    return c;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double defaultGold))
                    return (long)Math.Round(defaultGold * 10000.0);
            }
            else if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long val))
            {
                return val;
            }
            throw new ArgumentException("Could not parse currency amount '" + text + "'.");
        }

        void HandleResourceCommand(List<string> args)
        {
            var player = RequireKingdom();
            if (args.Count == 0 || string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
            {
                string filter = args.Count > 1 ? string.Join(" ", args.Skip(1)) : null;
                foreach (var item in ItemType.All.OfType<ResourceItemType>())
                {
                    string display = TranslateName(item.GetDisplayName());
                    if (!string.IsNullOrEmpty(filter)
                        && item.Identifier.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                        && display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    AppendLog("  " + item.Identifier + " (" + display + "): " + player.GetResourceAvailable(item).ToString("0.##", CultureInfo.InvariantCulture) + "/s");
                }
                return;
            }

            int startIndex = string.Equals(args[0], "add", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            if (args.Count - startIndex < 2)
            {
                AppendLog("Usage: resource <id|name|all> <amount> (or 'resource list [filter]')");
                return;
            }

            string amountToken = args[args.Count - 1];
            if (!double.TryParse(amountToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double amount))
            {
                AppendLog("Invalid resource amount '" + amountToken + "'.");
                return;
            }

            string target = string.Join(" ", args.Skip(startIndex).Take(args.Count - startIndex - 1)).Trim();
            if (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase))
            {
                int count = 0;
                foreach (var res in ItemType.All.OfType<ResourceItemType>())
                {
                    AddResourceAmount(player, res, amount);
                    count++;
                }
                AppendLog("Added " + amount.ToString("0.##", CultureInfo.InvariantCulture) + "/s to " + count + " resource type(s).");
                return;
            }

            var resolved = ResolveResource(target);
            if (resolved == null)
            {
                AppendLog("No resource found matching '" + target + "'. Try 'resource list'.");
                return;
            }

            AddResourceAmount(player, resolved, amount);
            AppendLog("Resource '" + TranslateName(resolved.GetDisplayName()) + "' (" + resolved.Identifier + ") quota is now "
                + player.GetResourceAvailable(resolved).ToString("0.##", CultureInfo.InvariantCulture) + "/s.");
        }

        static void AddResourceAmount(GamePlayer player, ResourceItemType resource, double amount)
        {
            if (resource is FarmItemType farm)
                player.SetFarmResource(farm, Math.Max(0.0, player.GetResourceAvailable(resource) + amount));
            else
                player.AddResource(resource, amount, false);
        }

        void HandleNightmareCommand(List<string> args)
        {
            var player = RequireKingdom();
            if (args.Count == 0 || !int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int points))
            {
                AppendLog("Usage: nightmare <points>");
                return;
            }
            player.NightmareCount = Math.Max(0, player.NightmareCount + points);
            player.NightmareCountActive = player.NightmareCount;
            AppendLog("Nightmare points: " + player.NightmareCount + " total (" + player.NightmareAvailable + " available).");
        }

        void HandleSpeedCommand(List<string> args)
        {
            if (args.Count == 0 || !float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) || scale < 0f || scale > 20f)
            {
                AppendLog("Usage: speed <0..20> (current: " + Time.timeScale.ToString("0.##", CultureInfo.InvariantCulture) + "x)");
                return;
            }
            Time.timeScale = scale;
            AppendLog("Time scale set to " + scale.ToString("0.##", CultureInfo.InvariantCulture) + "x.");
        }

        static TechNode ResolveTechNode(string query)
        {
            var exact = TechNode.Get(query);
            if (exact != null)
                return exact;
            var all = TechNode.Nodes.Where(n => !n.DevMode).ToList();
            return all.FirstOrDefault(n => string.Equals(n.Identifier, query, StringComparison.OrdinalIgnoreCase))
                ?? all.FirstOrDefault(n => string.Equals(TranslateName(n.Name), query, StringComparison.OrdinalIgnoreCase))
                ?? all.FirstOrDefault(n => n.Identifier.EndsWith("." + query, StringComparison.OrdinalIgnoreCase))
                ?? all.FirstOrDefault(n => n.Identifier.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? all.FirstOrDefault(n => TranslateName(n.Name).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static ResourceItemType ResolveResource(string query)
        {
            var resources = ItemType.All.OfType<ResourceItemType>().ToList();
            return resources.FirstOrDefault(r => string.Equals(r.Identifier, query, StringComparison.OrdinalIgnoreCase))
                ?? resources.FirstOrDefault(r => string.Equals(TranslateName(r.GetDisplayName()), query, StringComparison.OrdinalIgnoreCase))
                ?? resources.FirstOrDefault(r => r.Identifier.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? resources.FirstOrDefault(r => TranslateName(r.GetDisplayName()).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static GamePlayer RequireKingdom()
        {
            if (GamePlayer.Current == null || GamePlayer.Current.Flows.Count == 0)
                throw new InvalidOperationException("Load a kingdom first.");
            return GamePlayer.Current;
        }

        static string TranslateName(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";
            string translated = Translation.TranslateOnly(key);
            return string.IsNullOrEmpty(translated) ? key : translated;
        }

        static string FormatGold(long raw)
        {
            long abs = Math.Abs(raw);
            long g = abs / 10000L;
            long s = (abs / 100L) % 100L;
            long c = abs % 100L;
            return (raw < 0 ? "-" : "") + g + "g " + s + "s " + c + "c";
        }

        static List<string> SplitTokens(string input)
        {
            var result = new List<string>();
            int i = 0;
            while (i < input.Length)
            {
                while (i < input.Length && char.IsWhiteSpace(input[i]))
                    i++;
                if (i >= input.Length)
                    break;
                if (input[i] == '"' || input[i] == '\'')
                {
                    char quote = input[i++];
                    int start = i;
                    while (i < input.Length && input[i] != quote)
                        i++;
                    result.Add(input.Substring(start, i - start));
                    if (i < input.Length)
                        i++;
                }
                else
                {
                    int start = i;
                    while (i < input.Length && !char.IsWhiteSpace(input[i]))
                        i++;
                    result.Add(input.Substring(start, i - start));
                }
            }
            return result;
        }

        void AppendLog(string message)
        {
            log.Add(message);
            if (log.Count > 250)
                log.RemoveAt(0);
            scrollPosition.y = float.MaxValue;
        }
    }
}
