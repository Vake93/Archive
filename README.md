# Archive (.NET?)
Simple CLI application for archive files to azure blob storage

Usage:

```  archive [options]```

Options:
```
  --action <Setup|Download|Upload>  action
  --input <input>                   input
  --output <output>                 output
  --password <password>             password
```

## How to build?

Clone the repo and run
```dotnet publish -r <<RID>> -c Release```

Find the correct RID: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog

## How to use?

### Setup the Azure Blob Storage Account

```archive --action setup```

You will need to provide the connection string and an exisitng container name to upload the files into.

### Upload Files
```archive --action Upload --input .\Document.txt --output Document.enc --password P@$Sw0rd```

### Download Files
```archive --action Download --input Document.enc --Output .\Document.txt --password P@$Sw0rd```

## Things Todo:
- Upload and Download progress displays.
- Nicer error handling. EX: When passwords are incorrect
