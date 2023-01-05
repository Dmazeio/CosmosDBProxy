# Introduction 
Proxy for running integration tests on Windows (Docker Desktop for Windows), not needed on Linux/Mac.

# Getting Started
Build the image locally (from src folder):
```
docker build -t myproxy .
```

Then the integration tests should pick it up and create the container for you.