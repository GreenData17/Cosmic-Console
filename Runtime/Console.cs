using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Console : MonoBehaviour
{
    public static Console instance {get; private set;}

    // public
    public GameObject consoleOutputItem;
    public GameObject consoleOutputField;
    [Space]
    public TMP_InputField consoleInput;
    [Header("Settings")]
    public bool onCloseRePositon = true;
    private Vector3 originPos;

    // private~
    private bool isOpen;
    private CanvasGroup m_group;
    private List<GameObject> m_OutputObjectHistory = new List<GameObject>();
    private List<Command> Commands = new List<Command>();

    private void Awake(){
        if(instance is null) instance = this; else Destroy(gameObject);
        m_group = GetComponent<CanvasGroup>();
        isOpen = m_group.interactable;
        originPos = transform.position;

        AddCommand("help", "Shows the help list", Cmd_SendHelp);
        AddCommand("quit", "Quits the game.", Cmd_QuitGame);
        AddCommand("clear", "Clears the console.", Cmd_ClearHistory);

        SendWelcome();
    }

    private void Start() => CloseConsole();

    private void Update()
    {
        UpdateInput();

        if (Input.GetKey(KeyCode.F12) && !isOpen) OpenConsole();
        else if (Input.GetKey(KeyCode.F12) && isOpen) CloseConsole();
    }

    private void UpdateInput()
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
        string color = "#1647D0";

        SendToConsole("- Thank you for using CosmicConsole! -", color);

        #if UNITY_STANDALONE_WIN
        SendToConsole("CC is fully supported on windows!", color);
        #elif UNITY_STANDALONE_OSX
        SendToConsole("CC has not been tested on mac. use with care.", color);
        #elif UNITY_STANDALONE_LINUX
        SendToConsole("CC is partly supported on linux.", color);
        #elif UNITY_WEBGL
        SendToConsole("CC can cause some problems on webGL. use with care.", color);
        #elif UNITY_EDITOR
        SendToConsole("CC should run perfectly fine in the Editor.", color);
        #else
        SendToConsole("CC has not been tested on your device. use with care.", color);
        #endif

        SendToConsole("=================================", color);
    }

    // - Supscribe to the unity console -
    private void OnEnable() =>
        Application.logMessageReceived += HandleLog;

    private void OnDisable() =>
        Application.logMessageReceived -= HandleLog;


    // - Handle the vars recieved form unity console -
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (string.IsNullOrWhiteSpace(logString)) return;

        switch (type)
        {
            case LogType.Log:
                SendToConsole($"[LOG] {logString}");
                break;
            case LogType.Warning:
                SendToConsole($"[WARN] {logString}", "#DDDD00");
                break;
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                SendToConsole($"[ERROR] {logString}", "#DD0000");
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

        // if it doesn't exists send error message.
        if (!valid)
        {
            SendToConsole($"There is no Command with the alias \"{alias}\".", "#DD0000");
        }
    }

    // functions

    // - displays the text in the console output -
    public static void SendToConsole(string text, string colorInHex = "#FFFFFF"){
        if(string.IsNullOrWhiteSpace(text)) return;

        // remove all \n
        var temptext = text;
        text = temptext.Replace("\r", "").Replace("\n", "");

        // create output item
        var TempOutput = Instantiate(instance.consoleOutputItem, instance.consoleOutputField.transform);
        TempOutput.GetComponent<TMP_Text>().text = $"<color={colorInHex}>{text}";
        TempOutput.SetActive(true);

        // register item in history
        var consoleOutputScrollRect = instance.consoleOutputField.transform.parent.parent.gameObject.GetComponent<ScrollRect>();
        instance.m_OutputObjectHistory.Add(TempOutput);
        Canvas.ForceUpdateCanvases();
        consoleOutputScrollRect.verticalNormalizedPosition = 0;

        // [future may include save in a log file]
    }

    public void CloseConsole()
    {
        if(m_group.alpha == 0) return;

        m_group.alpha = 0;
        m_group.interactable = false;
        m_group.blocksRaycasts = false;
        StartCoroutine(OpenCloseDelay());

        if(onCloseRePositon)
            transform.position = originPos;
    }

    public void OpenConsole()
    {
        if(m_group.alpha == 1) return;

        m_group.alpha = 1;
        m_group.interactable = true;
        m_group.blocksRaycasts = true;
        StartCoroutine(OpenCloseDelay());
    }

    IEnumerator OpenCloseDelay(){
        yield return new WaitForSeconds(1f);
        isOpen = !isOpen;
    }


    // handle new commands and some examble commands

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
                "=================================",
                "=> <color=#1647D0>Thank you for using<color=#FFFFFF> CosmicConsole<color=#1647D0>!<color=#FFFFFF> <=",
            };

            foreach (string content in helpContent) { SendToConsole(content); }

            foreach (Command command in Commands)
            {
                if (string.IsNullOrEmpty(command.description)) continue;

                SendToConsole($"<color=#1647D0>{command.alias}<color=#FFFFFF> = <color=#DDDD00>{command.description}<color=#FFFFFF>");
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
                    SendToConsole("<color=#AA8800>There is no help defined for this command.<color=#FFFFFF>");
                    continue;
                }

                SendToConsole($"<color=#00DD00>{command.alias}<color=#FFFFFF> = <color=#DDDD00>{command.description}<color=#FFFFFF>");
            }
        }
    }

    // - clears the console -
    private void Cmd_ClearHistory(string[] args)
    {
        foreach(GameObject consoleOutputitem in m_OutputObjectHistory)
        {
            Destroy(consoleOutputitem);
        }
    }

    // - quits the game... or does it? -
    private void Cmd_QuitGame(string[] args)
    {
        Application.Quit();
        SendToConsole("Quitting Failed...", "#DD0000");
        SendToConsole("I'll never let you go! (~0o0)~ Uuuuuu~", "#AA0000");
    }

}
