# Copy client configuration

This guide shows you how to configure the Copy client, validate the configuration, and run it safely.

## Before you start

The application is a .NET 8 console app. It loads its configuration from JSON and supports a small set of environment variable overrides for secrets and deployment-specific values.

The runtime entrypoints are:

- `Copy.exe config` to generate a starter configuration
- `Copy.exe validate [path]` to validate a configuration file
- `Copy.exe [path]` to run the copy workflows

If you do not pass a path, the app looks for `config.json` in the current working directory.

## How to configure the client

### 1. Generate the starter files

Run the application with the `config` command:

```bash
Copy.exe config
```

This creates:

- `config.json`: a starter configuration with sample clients and tasks
- `scheme.json`: the generated JSON schema

> The code uses the word `scheme`, so the generated file is named `scheme.json`, even though it is effectively a schema file.

### 2. Define your clients

Add entries to the `Clients` array. Every client must have:

- `Type`
- `Name`
- `Host`

Supported client types are:

- `FTP`
- `SFTP`
- `Local`
- `Exchange`

Use these rules for each type:

| Client type | Required fields | Optional fields | Default port |
|---|---|---|---|
| `Local` | `Type`, `Name`, `Host` | `Port` | `0` |
| `FTP` | `Type`, `Name`, `Host`, `Username`, `Password` | `Port`, `Fingerprint` | `21` |
| `SFTP` | `Type`, `Name`, `Host`, `Username`, and either `Password` or `PrivateKey` | `Port`, `Fingerprint` | `22` |
| `Exchange` | `Type`, `Name`, `Host`, `Username`, `Password` | `Port`, `Autodiscover` | `443` |

Notes:

- `Name` is how tasks refer to a client.
- Client name matching is case-insensitive at runtime.
- `Fingerprint` is optional for FTP and SFTP. If you do not set it, the server certificate or host key is accepted automatically.
- `Autodiscover` only applies to `Exchange`.
- `Exchange` can be configured, but file operations in the current implementation are not implemented yet.

Example:

```json
{
  "Clients": [
    {
      "Type": "Local",
      "Name": "LocalInbox",
      "Host": "localhost"
    },
    {
      "Type": "SFTP",
      "Name": "PartnerSftp",
      "Host": "sftp.example.com",
      "Username": "svc-copy",
      "PrivateKey": "C:/keys/id_rsa",
      "Fingerprint": "11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00"
    }
  ]
}
```

### 3. Define your tasks

Add entries to the `Tasks` array. Every configuration must contain at least one task.

Each task must have:

- `Source`
- `Destination`

Each `Source`, `Destination`, and optional `MoveOriginalTo` object must have:

- `Client`: the name of a configured client
- `Path`: the folder path to use on that client

Available task fields:

| Field | Required | Purpose |
|---|---|---|
| `Id` | No | Stable workflow identifier used in logs |
| `Source` | Yes | Where files are read from |
| `Destination` | Yes | Where files are written to |
| `Delete` | No | Delete the source file after a successful transfer |
| `MoveOriginalTo` | No | Move the source file after a successful transfer |
| `Filter` | No | Restrict which files are processed |
| `Zip` | No | Enable zip output |
| `ZipFileName` | No | Custom zip filename |
| `Overwrite` | No | Replace existing destination files; defaults to `true` |

Rules enforced by validation:

- `Delete` and `MoveOriginalTo` cannot be used together in the same task.
- Referenced clients must already exist in `Clients`.
- Task IDs must be unique when you provide them.

Filter fields:

| Field | Default | Purpose |
|---|---|---|
| `Name` | `.*` | Regular expression applied to file names |
| `Author` | `.*` | Regular expression applied to file owner/author |
| `CreatedAfter` | minimum date | Only process newer files; accepts either a `DateTime` or a `TimeSpan` string |
| `MaxSize` | maximum unsigned integer | Maximum file size in bytes |
| `MinSize` | `0` | Minimum file size in bytes |

Notes:

- SFTP logs a warning and ignores `Filter.Author`, because author filtering is not supported for that client.
- `CreatedAfter` supports both absolute date/time values such as `2026-01-01T00:00:00` and relative `TimeSpan` values such as `-1.00:00:00` or `-12:00:00`.
- When you use a `TimeSpan`, the application adds it to the current local time. In practice, use a negative value to mean “files created within the last period”.
- Local source directories must already exist.
- Destination directories are created automatically for Local, FTP, and SFTP when needed.

Example:

```json
{
  "Tasks": [
    {
      "Id": "local-to-sftp-reports",
      "Source": {
        "Client": "LocalInbox",
        "Path": "C:/Copy/inbox"
      },
      "Destination": {
        "Client": "PartnerSftp",
        "Path": "/upload"
      },
      "MoveOriginalTo": {
        "Client": "Archive",
        "Path": "C:/Copy/archive"
      },
      "Filter": {
        "Name": "^report_.*\\.csv$",
        "CreatedAfter": "-1.00:00:00",
        "MaxSize": 52428800,
        "MinSize": 1
      },
      "Overwrite": true
    }
  ]
}
```

If you use `MoveOriginalTo`, make sure the referenced archive client also exists in `Clients`.

### 4. Configure email notifications when needed

If you want warning and error notifications, set `MailTo` and `Smtp`.

Rules enforced by validation:

- If `MailTo` contains recipients, `Smtp` must be configured.
- `Smtp.Host`, `Smtp.Port`, and `Smtp.From` are required.
- `Smtp.Username` and `Smtp.Password` must be provided together.
- Email addresses in `MailTo` and `Smtp.From` must be valid.

Example:

```json
{
  "MailTo": [
    "ops@example.com",
    "support@example.com"
  ],
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "smtp-user",
    "Password": "smtp-password",
    "From": "copy@example.com",
    "EnableSsl": true
  }
}
```

### 5. Keep secrets in environment variables when deploying

The application supports targeted overrides from environment variables. This is the safest way to inject passwords, private keys, or deployment-specific hosts without changing `config.json`.

General variables:

| Variable | Purpose |
|---|---|
| `DEBUG` | Enables debug logging when set to `true` |
| `CONFIG_PATH` | Defined in code and the Docker image as a config path value |
| `SCHEME_PATH` | Defined in code and the Docker image as a schema path value |

Client overrides use this pattern:

```text
COPY_CLIENT_<CLIENT_NAME_UPPERCASE>_HOST
COPY_CLIENT_<CLIENT_NAME_UPPERCASE>_USERNAME
COPY_CLIENT_<CLIENT_NAME_UPPERCASE>_PASSWORD
COPY_CLIENT_<CLIENT_NAME_UPPERCASE>_PRIVATEKEY
COPY_CLIENT_<CLIENT_NAME_UPPERCASE>_PORT
```

Example for a client named `PartnerSftp`:

```text
COPY_CLIENT_PARTNERSFTP_HOST=sftp.prod.example.com
COPY_CLIENT_PARTNERSFTP_USERNAME=svc-copy
COPY_CLIENT_PARTNERSFTP_PRIVATEKEY=/run/secrets/id_rsa
COPY_CLIENT_PARTNERSFTP_PORT=22
```

SMTP overrides:

```text
COPY_SMTP_HOST
COPY_SMTP_USERNAME
COPY_SMTP_PASSWORD
COPY_SMTP_FROM
COPY_SMTP_PORT
COPY_SMTP_ENABLESSL
```

Important runtime note:

- `CONFIG_PATH` and `SCHEME_PATH` are defined in `Config.cs` and in the Docker image.
- The main startup flow in `Program.cs` still loads the configuration from the first CLI argument, or from `config.json` if no argument is provided.
- In practice, pass the config path explicitly when you want to be certain which file is loaded.

### 6. Validate the configuration

Validate before running:

```bash
Copy.exe validate config.json
```

For a custom path:

```bash
Copy.exe validate C:/Copy/config.production.json
```

Validation checks include:

- required client fields
- required task fields
- missing or duplicated task IDs
- missing client references in tasks
- invalid email addresses
- incomplete SMTP credentials
- incompatible task options such as `Delete` plus `MoveOriginalTo`

### 7. Run the client

Run with the default file:

```bash
Copy.exe
```

Run with an explicit path:

```bash
Copy.exe C:/Copy/config.production.json
```

## Full example configuration

```json
{
  "Debug": false,
  "MailTo": [
    "ops@example.com"
  ],
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "smtp-user",
    "Password": "smtp-password",
    "From": "copy@example.com",
    "EnableSsl": true
  },
  "Clients": [
    {
      "Type": "Local",
      "Name": "LocalInbox",
      "Host": "localhost"
    },
    {
      "Type": "Local",
      "Name": "Archive",
      "Host": "localhost"
    },
    {
      "Type": "FTP",
      "Name": "PartnerFtp",
      "Host": "ftp.example.com",
      "Username": "ftp-user",
      "Password": "ftp-password",
      "Fingerprint": "AA11BB22CC33DD44EE55FF6677889900AA11BB22"
    },
    {
      "Type": "SFTP",
      "Name": "PartnerSftp",
      "Host": "sftp.example.com",
      "Username": "svc-copy",
      "PrivateKey": "C:/keys/id_rsa",
      "Fingerprint": "11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00"
    }
  ],
  "Tasks": [
    {
      "Id": "local-to-ftp-json",
      "Source": {
        "Client": "LocalInbox",
        "Path": "C:/Copy/inbox"
      },
      "Destination": {
        "Client": "PartnerFtp",
        "Path": "/incoming"
      },
      "Delete": true,
      "Filter": {
        "Name": "^.*\\.json$",
        "Author": ".*",
        "CreatedAfter": "2026-01-01T00:00:00",
        "MaxSize": 10485760,
        "MinSize": 1
      },
      "Overwrite": true
    },
    {
      "Id": "local-to-sftp-reports",
      "Source": {
        "Client": "LocalInbox",
        "Path": "C:/Copy/outbox"
      },
      "Destination": {
        "Client": "PartnerSftp",
        "Path": "/upload"
      },
      "MoveOriginalTo": {
        "Client": "Archive",
        "Path": "C:/Copy/archive"
      },
      "Filter": {
        "Name": "^report_.*\\.csv$",
        "CreatedAfter": "2026-01-01T00:00:00",
        "MaxSize": 52428800,
        "MinSize": 1
      },
      "Overwrite": true
    }
  ]
}
```

## Docker note

The Docker image defines these environment variables by default:

```text
CONFIG_PATH=/app/config/config.json
SCHEME_PATH=/app/config/scheme.json
DEBUG=false
```

It also creates and exposes `/app/config` as a volume.

Because the application entrypoint is `Copy.exe`, pass the config file path explicitly if your container runtime does not launch the process from the directory that contains `config.json`.

## Practical limits to keep in mind

- There must be at least one client and at least one task.
- Local source folders must already exist.
- FTP, SFTP, and Local destinations are created automatically when needed.
- If `Overwrite` is `false`, existing destination files are skipped.
- If `MoveOriginalTo` points to the same client as the source, the file is moved in place; otherwise the file is copied to the archive location and then deleted from the source.
- Exchange authentication can be configured, but file operations currently throw `NotImplementedException`.
