# Npgsql Sm3 Auth builder

## Steps

### 1. Install the package

```
dotnet tool install -g Npgsql.Sm3Auth.Builder
```

### 2. Clone Npgsql repository

```
mkdir ~/github && cd ~/github && git clone https://github.com/npgsql/npgsql.git
```

### 3. Run the builder

```
buildpgsm3 --version v9.0.3 --source /Users/lewis/github/npgsql --output /Users/lewis/Downloads
```