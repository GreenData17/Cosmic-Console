# Cosmic-Console
CC is a Unity package adding a in-game Console for debugging and controlling a game, outside of the Unity Editor.

![cosmic console image](https://raw.githubusercontent.com/GreenData17/Cosmic-Console/main/Runtime/Cosmic-console-image.png)

CC automatically gets the logs from unity. calling Unity's log functions is all it needs.
```C#
// Unity's log functions
Debug.Log(object message, object context);
Debug.LogWarning(object message, object context);
Debug.LogError(object message, object context);
...
```

Commands can be created using:
```C#
// makes a Command with a description.
Console.AddCommand(string alias, string description, Action<string[]> methodeToExecute);

// makes a command without a description. Commands created using this methode will not appear in the help command.
Console.AddCommand(string alias, Action<string[]> methodeToExecute);

// This is used to remove a command. This is usually not required to be called.
Console.RemoveCommand(string alias);
```
