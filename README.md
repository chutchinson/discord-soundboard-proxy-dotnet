# discord-soundboard-proxy-dotnet

Application with a tray icon that sends commands to a proxy. The proxy is hosted in an AWS Lambda and relays commands to a soundboard bot in the Lab 2636 Discord server.

This application likely isn't useful to anyone except for those in the Lab 2635 group, sorry about that! Feel free to learn from the code or use it for your own projects.

# Caveats

This bot was written for a small circle of friends and suffers from a lack of documentation, no warranty of any kind, and is not representative of quality work.

# Supported Platforms

- Windows 10

# Dependencies

- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Binder
- Microsoft.Extensions.Configuration.Json
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Options
- Newtonsoft.Json
- Serilog
- Serilog.Sinks.File

# Configuration

Create a **config.json** file in the working directory of the application.

All configuration options are required except for key bindings.

| Key                 | Description                          | Default |
| ------------------- | ------------------------------------ | ------- |
| commandUri          | URI template for AWS Lambda bot      | n/a     |
| bindings            | Dictionary of key bindings           | n/a     |

### commandUri

Must be a path with a (zero) positional string argument (.NET String.Format) that points to the **play** endpoint of an AWS Lambda function specifically designed for this application. Ask around in the Lab 2635 Discord group for additional information.

## Configuration Example

```json
{
    "commandUri": "https://soundboard.example.org/play/{0}",
    "bindings": {
        "lctrl+lshift+g": "goal"
    }
}
```

# Key Bindings

The following keys are available for binding:

    # letters / digits
    a b c d e f g h i j k l m n o p q r s t u v w x y z
    0 1 2 3 4 5 6 7 8 9
    
    # number pad
    n0 n1 n2 n3 n4 n5 n6 n7 n8 n9

    # function keys
    f1 f2 f3 f4 f5 f6 f7 f8 f9 f10 f11 f12

    # arrow keys
    left up right down

    # modifiers
    lctrl lmenu lshift rctrl rmenu rshift

    # utilities
    pgup pgdn enter bksp space esc

Modifiers can be combined with +:

    lctrl+lshift+a
    lmenu+f4
   
