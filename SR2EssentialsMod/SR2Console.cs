﻿using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using Il2CppTMPro;
using MelonLoader;
using SR2E.Commands;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SR2E
{
    public static class SR2Console
    {

        /// <summary>
        /// Display a message in the console
        /// </summary>
        public static void SendMessage(string message)
        {
            if(!SR2EMain.consoleFinishedCreating)
                return;
            if (consoleContent.childCount >= maxMessages)
                GameObject.Destroy(consoleContent.GetChild(0).gameObject);
            if (message.Contains("\n"))
            {
                foreach (string singularLine in message.Split('\n'))
                    SendMessage(singularLine);
                return;
            }
            var instance = GameObject.Instantiate(messagePrefab, consoleContent);
            instance.gameObject.SetActive(true);
            instance.text = message;
            _scrollbar.value = 0f;
            scrollCompletlyDown = true;
        }
        /// <summary>
        /// Display an error in the console
        /// </summary>
        public static void SendError(string message)
        {
            if(!SR2EMain.consoleFinishedCreating)
                return;
            if (consoleContent.childCount >= maxMessages)
                GameObject.Destroy(consoleContent.GetChild(0).gameObject);
            if (message.Contains("\n"))
            {
                foreach (string singularLine in message.Split('\n'))
                    SendError(singularLine);
                return;
            }
            var instance = GameObject.Instantiate(messagePrefab, consoleContent);
            instance.gameObject.SetActive(true);
            instance.text = message;
            instance.color = new Color(0.6f, 0, 0, 1);
            _scrollbar.value = 0f;
            scrollCompletlyDown = true;
        }
        /// <summary>
        /// Check if console is open
        /// </summary>
        public static bool isOpen
        { get { return transform.GetChild(0).gameObject.activeSelf; } }

        /// <summary>
        /// Closes the console
        /// </summary>
        public static void Close()
        {
            for (int i = 0; i < autoCompleteContent.childCount; i++)
            {
                Object.Destroy(autoCompleteContent.GetChild(i).gameObject);
            }
            if (Object.FindObjectsOfType<MapUI>().Length != 0)
                return;
            consoleBlock.SetActive(false);
            consoleMenu.SetActive(false);
            if (shouldResetTime)
                normalTimeScale = 1;
            Time.timeScale = normalTimeScale;
            Object.FindObjectOfType<InputSystemUIInputModule>().actionsAsset.Enable();

        }
        
        /// <summary>
        /// Opens the console
        /// </summary>
        public static void Open()
        { 
            if (SR2ModMenu.isOpen)
                return;
            
            Il2CppArrayBase<MapUI> allMapUIs = Object.FindObjectsOfType<MapUI>();
            for (int i = 0; i < allMapUIs.Count; i++)
                Object.Destroy(allMapUIs[i].gameObject);
            shouldResetTime = allMapUIs.Count != 0;
            
            consoleBlock.SetActive(true);
            consoleMenu.SetActive(true);
            normalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Object.FindObjectOfType<InputSystemUIInputModule>().actionsAsset.Disable();
            RefreshAutoComplete(commandInput.text);
        }
        /// <summary>
        /// Toggles the console
        /// </summary>
        public static void Toggle()
        {
            if(isOpen)
                Close();
            else
                Open();
        }
        /// <summary>
        /// Registers a command to be used in the console
        /// </summary>
        public static bool RegisterCommand(SR2CCommand cmd)
        {
            if (commands.ContainsKey(cmd.ID.ToLowerInvariant()))
            {
                SendMessage($"Trying to register command with id '<color=white>{cmd.ID.ToLowerInvariant()}</color>' but the ID is already registered!");
                return false;
            }
            commands.Add(cmd.ID.ToLowerInvariant(), cmd);
            List<KeyValuePair<string, SR2CCommand>> myList = commands.ToList();

            myList.Sort(delegate(KeyValuePair<string, SR2CCommand> pair1, KeyValuePair<string, SR2CCommand> pair2) { return pair1.Key.CompareTo(pair2.Key); });
            commands = myList.ToDictionary(x => x.Key, x => x.Value);
            return true;
        }

        /// <summary>
        /// Execute a string as if it was a commandId with args
        /// </summary>
        public static void ExecuteByString(string input)
        {
            string[] cmds = input.Split(';');
            foreach (string c in cmds)
                if (!String.IsNullOrEmpty(c))
                {
                    bool spaces = c.Contains(" ");
                    string cmd = spaces ? c.Substring(0, c.IndexOf(' ')) : c;
                
                    if (commands.ContainsKey(cmd))
                    {
                        bool successful;
                        if (spaces)
                        {
                            var argString = c.TrimEnd()+" ";
                            List<string> split = argString.Split(' ').ToList();
                            split.RemoveAt(0);
                            split.RemoveAt(split.Count-1);
                            successful = commands[cmd].Execute(split.ToArray());
                        }
                        else
                            successful = commands[cmd].Execute(null);
                    }
                    else
                        SendError("Unknown command. Please use '<color=white>help</color>' for available commands");
                }
        }
        
        

        internal static Transform transform;
        internal static GameObject gameObject;
        internal static Dictionary<string, SR2CCommand> commands = new Dictionary<string, SR2CCommand>();
        internal static T getObjRec<T>(Transform transform, string name) where T : class
        {
            List<GameObject> totalChildren = getAllChildren(transform);
            for (int i = 0; i < totalChildren.Count; i++)
                if(totalChildren[i].name==name)
                {
                    if (typeof(T) == typeof(GameObject))
                        return totalChildren[i] as T;
                    if (typeof(T) == typeof(Transform))
                        return totalChildren[i].transform as T;
                    if (totalChildren[i].GetComponent<T>() != null)
                        return totalChildren[i].GetComponent<T>();
                }
            return null;
        }

        static List<GameObject> getAllChildren(Transform container)
        {
            List<GameObject> allChildren = new List<GameObject>();
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                allChildren.Add(child.gameObject);
                allChildren.AddRange(getAllChildren(child));
            }
            return allChildren;
        }
        
        static Scrollbar _scrollbar;
        static float normalTimeScale = 1f;
        static bool shouldResetTime = false;
        const int maxMessages = 100;
        private static bool scrollCompletlyDown = false;

        static void RefreshAutoComplete(string text)
        {
            for (int i = 0; i < autoCompleteContent.childCount; i++)
                Object.Destroy(autoCompleteContent.GetChild(i).gameObject);
            if (String.IsNullOrEmpty(text))
            { autoCompleteScrollView.SetActive(false); return; }
            if (text.Contains(" "))
            {
                string cmd = text.Substring(0, text.IndexOf(' '));
                if (commands.ContainsKey(cmd))
                {
                    var argString = text;
                    List<string> split = argString.Split(' ').ToList();
                    split.RemoveAt(0);
                    int argIndex = split.Count-1;
                    string[] args = null;
                    if (split.Count!=0)
                        args = split.ToArray();
                    List<string> possibleAutoCompletes = (commands[cmd].GetAutoComplete(argIndex, args));
                    if (possibleAutoCompletes != null)
                    {
                        int maxPredictions = 20; //This is to reduce lag
                        int predicted = 0;
                        foreach (string argument in possibleAutoCompletes)
                        {
                            if (predicted > maxPredictions)
                                return;
                            if (args != null)
                                if (!argument.ToLower().StartsWith(split[split.Count - 1].ToLower()))
                                    continue;
                            predicted++;
                            GameObject instance = Object.Instantiate(autoCompleteEntryPrefab, autoCompleteContent);
                            instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = argument;
                            instance.SetActive(true);
                            instance.GetComponent<Button>().onClick.AddListener((Action)(() =>
                            {
                                commandInput.text = cmd;

                                if (args != null)
                                {
                                    for (int i = 0; i < args.Length - 1; i++)
                                    {
                                        commandInput.text += " " + args[i];
                                    }

                                    commandInput.text += " " + argument;
                                }

                                commandInput.MoveToEndOfLine(false, false);
                            }));
                        }
                    }
                }
            }
            else
                foreach (KeyValuePair<string, SR2CCommand> valuePair in commands)
                    if (valuePair.Key.StartsWith(text))
                    {
                        GameObject instance = Object.Instantiate(autoCompleteEntryPrefab, autoCompleteContent);
                        instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = valuePair.Key;
                        instance.SetActive(true); 
                        instance.GetComponent<Button>().onClick.AddListener((Action)(() =>
                        {
                            commandInput.text = valuePair.Key;
                            commandInput.MoveToEndOfLine(false, false);
                        }));
                    }
            autoCompleteScrollView.SetActive(autoCompleteContent.childCount!=0);
        }

        internal static void Start()
        {
            consoleBlock = getObjRec<GameObject>(transform,"consoleBlock");
            consoleMenu = getObjRec<GameObject>(transform,"consoleMenu");
            consoleContent = getObjRec<Transform>(transform, "ConsoleContent");
            messagePrefab = getObjRec<TextMeshProUGUI>(transform, "messagePrefab");
            commandInput = getObjRec<TMP_InputField>(transform, "commandInput");
            _scrollbar = getObjRec<Scrollbar>(transform,"ConsoleScroll");
            autoCompleteContent = getObjRec<Transform>(transform, "AutoCompleteContent");
            autoCompleteEntryPrefab = getObjRec<GameObject>(transform, "AutoCompleteEntry");
            autoCompleteScrollView = getObjRec<GameObject>(transform, "AutoCompleteScroll");
            
            autoCompleteScrollView.SetActive(false);
            consoleBlock.SetActive(false);
            consoleMenu.SetActive(false);
            commandInput.onValueChanged.AddListener((Action<string>)((text) => {RefreshAutoComplete(text); }));
            RegisterCommand(new GiveCommand());
            RegisterCommand(new BindCommand());
            RegisterCommand(new UnbindCommand());
            RegisterCommand(new SpawnCommand());
            RegisterCommand(new FastForwardCommand());
            RegisterCommand(new ClearCommand());
            RegisterCommand(new ClearInventoryCommand());
            RegisterCommand(new ModsCommand());
            RegisterCommand(new HelpCommand());
            RegisterCommand(new RefillSlotsCommand());
            RegisterCommand(new NewBucksCommand());
            RegisterCommand(new KillCommand());
            RegisterCommand(new GiveGadgetCommand());
            RegisterCommand(new GiveBlueprintCommand());
            
            //Disabled do to not working yet
            //RegisterCommand(new NoClipCommand());
            
            bool hasInfiniteEnergyMod = false;
            foreach (MelonBase melonBase in MelonBase.RegisteredMelons)
            {
                if (melonBase.ID == "InfiniteEnergy")
                    hasInfiniteEnergyMod = true;
            }

            if (!hasInfiniteEnergyMod)
            {
                RegisterCommand(new InfiniteEnergyCommand());
            }
            
            
            SR2CommandBindingManager.Start();
            //Setup Modmenu
            
            SR2ModMenu.parent = transform;
            SR2ModMenu.gameObject = getObjRec<GameObject>(transform,"modMenu");
            SR2ModMenu.transform = getObjRec<Transform>(transform,"modMenu");
            SR2ModMenu.Start();
        }

        static TMP_InputField commandInput;
        private static GameObject autoCompleteEntryPrefab;
        static GameObject consoleBlock;
        static GameObject consoleMenu;
        static Transform consoleContent;
        static Transform autoCompleteContent;
        static GameObject autoCompleteScrollView;
        static TextMeshProUGUI messagePrefab;
        internal static void Update()
        {
            if (SR2EMain.consoleFinishedCreating != true)
                return;
            commandInput.ActivateInputField();
            if (isOpen)
            {
                if (scrollCompletlyDown)
                    if (_scrollbar.value != 0)
                    {
                        _scrollbar.value = 0f;
                        scrollCompletlyDown = false;
                    }

                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    if (autoCompleteContent.childCount != 0)
                    {
                        //Select first to autocomplete
                        autoCompleteContent.GetChild(0).GetComponent<Button>().onClick.Invoke();
                    }
                }
                
                if (Keyboard.current.enterKey.wasPressedThisFrame)
                    if(commandInput.text!="")
                        Execute();
                if (Time.timeScale!=0f)
                    Time.timeScale=0;
            }
            if (Keyboard.current.ctrlKey.wasPressedThisFrame)
                if(Keyboard.current.tabKey.isPressed)
                    Toggle();
            if (Keyboard.current.tabKey.wasPressedThisFrame)
                if(Keyboard.current.ctrlKey.isPressed)
                    Toggle();
            
            if (_scrollbar != null)
            {
                float value = Mouse.current.scroll.ReadValue().y;
                if (Mouse.current.scroll.ReadValue().y!=0)
                    _scrollbar.value = Mathf.Clamp(_scrollbar.value+((value > 0.01 ? 1.25f : value < -0.01 ? -1.25f : 0) * _scrollbar.size),0,1f);

            }
            SR2CommandBindingManager.Update();
            //Modmenu
            SR2ModMenu.Update();
        }
        

        static void Execute()
        {
            string cmds = commandInput.text;
            commandInput.text = "";
            for (int i = 0; i < autoCompleteContent.childCount; i++)
            {
                Object.Destroy(autoCompleteContent.GetChild(i).gameObject);
            }
            ExecuteByString(cmds);
            
        }


        
    }
}