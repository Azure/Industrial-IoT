# OPC Publisher configuration via command line options and environment variables

[Home](./readme.md)

> This documentation applies to version 2.9

The following OPC Publisher configuration can be applied by Command Line Interface (CLI) options or as environment variable settings. Any CamelCase options can also be provided using environment variables (without the preceding `--`).

> IMPORTANT The command line of OPC Publisher only understands below command line options. You cannot specify environment variables on the command line (e.g., like `env1=value env2=value`). All option names are **case-sensitive**!

When both environment variable and CLI argument are provided, the command line option will override the environment variable.

```text
```

Currently supported combinations of `--mm` snd `--me` can be found [here](./messageformats.md).
