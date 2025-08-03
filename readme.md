# Npgsql Sm3 Auth builder

[![.NET](https://github.com/zlzforever/NpgsqlSm3AuthBuilder/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/zlzforever/NpgsqlSm3AuthBuilder/actions/workflows/dotnet.yml)

Build Npgsql dotent driver support SM3 auth

## Steps

### 1. Install the package

```
dotnet tool install --global NpgsqlSm3AuthBuilder
```

### 2. Clone Npgsql repository

```
mkdir ~/github && cd ~/github && git clone https://github.com/npgsql/npgsql.git
```

### 3. Run the builder

```
buildpgsm3 --version v9.0.3 --source /Users/lewis/github/npgsql --output /Users/lewis/Downloads
```
