using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace cosmicconsole{

    public class Console : MonoBehaviour
    {
        public static Console instance {get; private set;}

        // public
        public GameObject consoleOutputItem;
        public GameObject consoleOutputField;
        [Space]
        public TMP_InputField consoleInput;
        [Header("Settings")]
        public bool printWelcomeOnAwake = true;
        public bool printUnityConsole = true;
        public bool onCloseRePositon = true;
        private Vector3 _originPos;

        [Header("Colors")]
        public Color normalLog = new Color(1,1,1,1);
        public Color warningLog = new Color(1,1,0,1);
        public Color errorLog = new Color(1,0.5f,0.5f,1);
        public Color infoLog = new Color(0,1,1,1);
        public EventHandler<ConsoleStateArgs> OnConsoleStateChange;

        // private
        private bool isOpen;
        private CanvasGroup _group;
        private List<GameObject> _OutputObjectHistory = new List<GameObject>();
        private List<Command> Commands = new List<Command>();

        private void Awake(){
            if(instance is null) instance = this; else Destroy(gameObject);
            _group = GetComponent<CanvasGroup>();
            isOpen = _group.interactable;
            _originPos = transform.localPosition;

            AddCommand("help", "Shows the help list", Cmd_SendHelp);
            AddCommand("quit", "Quits the game.", Cmd_QuitGame);
            AddCommand("clear", "Clears the console. (\"cls\" works too)", Cmd_ClearHistory);
            AddCommand("cls", Cmd_ClearHistory);

            if(printWelcomeOnAwake) SendWelcome();
        }

        private void Start() => CloseConsole();

        private void Update()
        {
            ResetInputField();

            if (Input.GetKey(KeyCode.F12) && !isOpen) OpenConsole();
            else if (Input.GetKey(KeyCode.F12) && isOpen) CloseConsole();
        }

        private void ResetInputField()
        {
            if (!string.IsNullOrEmpty(consoleInput.text))
            {
                // get input and reset InputField
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    HandleInput(consoleInput.text);
                    consoleInput.text = "";
                    consoleInput.Select();
                    consoleInput.ActivateInputField();
                }
            }
        }

        // - sends a welcome message -
        private void SendWelcome(){
            Color color = infoLog;

            SendToConsole("- Thank you for using CosmicConsole! -", color);

            #if UNITY_EDITOR
            SendToConsole("CC should run perfectly fine in the Editor.", color);
            #elif UNITY_STANDALONE_WIN
            SendToConsole("CC is fully supported on windows!", color);
            #elif UNITY_STANDALONE_OSX
            SendToConsole("CC has not been tested on mac.", color);
            #elif UNITY_STANDALONE_LINUX
            SendToConsole("CC is partly supported on linux.", color);
            #elif UNITY_WEBGL
            SendToConsole("CC can cause some problems on webGL.", color);
            #else
            SendToConsole("CC has not been tested on your device.", color);
            #endif

            SendToConsole("=================================", color);
        }

        // - Supscribe to the unity console -
        private void OnEnable() =>
            Application.logMessageReceived += HandleLog;

        private void OnDisable() =>
            Application.logMessageReceived -= HandleLog;


        // - Handle the data recieved form unity console -
        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!printUnityConsole) return;
            if (string.IsNullOrWhiteSpace(logString)) return;

            switch (type)
            {
                case LogType.Log:
                    SendToConsole($"[LOG] {logString}");
                    break;
                case LogType.Warning:
                    SendToConsole($"[WARN] {logString}", warningLog);
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    SendToConsole($"[ERROR] {logString}", errorLog);
                    break;
                default:
                    SendToConsole(logString);
                    break;
            }
        }

        // - check if input consists of multiple words and send them as arguments -
        private void HandleInput(string input)
        {
            var arguments = input.Split(' ');

            if (arguments.Length > 1)
            {
                List<string> argumentsSeperated = input.Split(' ').ToList();
                argumentsSeperated.RemoveAt(0);
                
                CallCommand(arguments[0], argumentsSeperated.ToArray());
            }
            else CallCommand(arguments[0], new string[] { });
        }

        // - checks if command exists and executes it if true -
        private void CallCommand(string alias, string[] arguments)
        {
            bool valid = false;

            //search for the command and execute it.
            foreach (Command command in Commands)
            {
                if (alias != command.alias) continue;

                command.method(arguments);
                valid = true;
            }

            // if command does not exists send error message.
            if (!valid)
            {
                SendToConsole($"There is no Command with the alias \"{alias}\".",  errorLog);
            }
        }

        public static string ColorToHex(Color color){
            return ColorUtility.ToHtmlStringRGBA(color);
        }

        // - displays text in console output -
        public static void SendToConsole(string text, string colorInHex = "FFFFFF"){
            if(string.IsNullOrWhiteSpace(text)) return;

            // remove all \n
            var temptext = text;
            text = temptext.Replace("\r", "").Replace("\n", "");

            // create output item
            var TempOutput = Instantiate(instance.consoleOutputItem, instance.consoleOutputField.transform);
            TempOutput.GetComponent<TMP_Text>().text = $"<color=#{colorInHex}>{text}";
            TempOutput.SetActive(true);

            // register item in history
            var consoleOutputScrollRect = instance.consoleOutputField.transform.parent.parent.gameObject.GetComponent<ScrollRect>();
            instance._OutputObjectHistory.Add(TempOutput);
            Canvas.ForceUpdateCanvases();
            consoleOutputScrollRect.verticalNormalizedPosition = 0;

            // TODO: [future may include save in a log file]
        }

        public static void SendToConsole(string text, Color color){
            string hexColor = ColorUtility.ToHtmlStringRGBA(color);
            SendToConsole(text, hexColor);
        }

        public void CloseConsole()
        {
            if(_group.alpha == 0) return;

            _group.alpha = 0;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            ConsoleStateChanged(false);
            StartCoroutine(OpenCloseDelay());

            if(onCloseRePositon)
                transform.localPosition = _originPos;
        }

        public void OpenConsole()
        {
            if(_group.alpha == 1) return;

            _group.alpha = 1;
            _group.interactable = true;
            _group.blocksRaycasts = true;
            ConsoleStateChanged(true);
            StartCoroutine(OpenCloseDelay());
        }

        IEnumerator OpenCloseDelay(){
            yield return new WaitForSeconds(1f);
            isOpen = !isOpen;
        }

        void ConsoleStateChanged(bool isOpen){
            EventHandler<ConsoleStateArgs> handler = OnConsoleStateChange;
            handler?.Invoke(this, new ConsoleStateArgs {open = isOpen});
        }

        public class ConsoleStateArgs : EventArgs{
            public bool open { get; set; }
        }


        // handle new commands and some example commands

        // - add command without description. (will be hidden in help list) -
        public static void AddCommand(string alias, Action<string[]> methodeToExecute)
        {
            if (methodeToExecute.GetMethodInfo().GetParameters().Length == 0) return;

            instance.Commands.Add(new Command() { alias = alias, method = methodeToExecute });
        }

        // - add command with description. -
        public static void AddCommand(string alias, string description, Action<string[]> methodeToExecute)
        {
            if (methodeToExecute.GetMethodInfo().GetParameters().Length == 0) return;

            instance.Commands.Add(new Command() { alias = alias, description = description, method = methodeToExecute });
        }

        // - removes a command -
        public static void RemoveCommand(string alias)
        {
            foreach(Command command in instance.Commands)
            {
                if (command.alias != alias) continue;
                instance.Commands.Remove(command);
            }
        }

        // commands

        // - prints a list of all commands (if they have an description) -
        private void Cmd_SendHelp(string[] args)
        {
            if (args.Length == 0) // show help list
            {
                string[] helpContent =
                {
                    $"=================================",
                    $"=> <color=#{ColorToHex(infoLog)}>"+
                    $"Thank you for "+
                    $"using<color=#{ColorToHex(normalLog)}> "+
                    $"CosmicConsole"+
                    $"<color=#{ColorToHex(infoLog)}>"+
                    $"!"+
                    $"<color=#{ColorToHex(normalLog)}> <=",
                };

                foreach (string content in helpContent) { SendToConsole(content); }

                foreach (Command command in Commands)
                {
                    if (string.IsNullOrEmpty(command.description)) continue;

                    SendToConsole(
                        $"<color=#{ColorToHex(infoLog)}>"+
                        $"{command.alias}"+
                        $"<color=#{ColorToHex(normalLog)}>"+
                        $" = "+
                        $"<color=#{ColorToHex(warningLog)}>"+
                        $"{command.description}"+
                        $"<color=#{ColorToHex(normalLog)}>");
                }
                SendToConsole("=================================");
            }
            else // search help for a specifide command
            {
                foreach (Command command in Commands)
                {
                    if (command.alias != args[0]) continue;

                    if (string.IsNullOrEmpty(command.description))
                    {
                        SendToConsole(
                            $"<color=#{ColorToHex(warningLog)}>"+
                            $"There is no help defined for this command."+
                            $"<color=#{ColorToHex(normalLog)}>");
                        continue;
                    }

                    SendToConsole(
                        $"<color=#{ColorToHex(infoLog)}>"+
                        $"{command.alias}"+
                        $"<color=#{ColorToHex(normalLog)}>"+
                        $" = "+
                        $"<color=#{ColorToHex(warningLog)}>"+
                        $"{command.description}"+
                        $"<color=#{ColorToHex(normalLog)}>");
                }
            }
        }

        // - clears the console -
        private void Cmd_ClearHistory(string[] args)
        {
            foreach(GameObject consoleOutputitem in _OutputObjectHistory)
            {
                Destroy(consoleOutputitem);
            }
        }

        // - quits the game... or does it? -
        private void Cmd_QuitGame(string[] args)
        {
            Application.Quit();
            SendToConsole("Quitting Failed...", errorLog);
            //SendToConsole("I'll never let you go! (~0o0)~ Uuuuuu~", infoLog); //Some old function I made for fun
        }

    }
}
