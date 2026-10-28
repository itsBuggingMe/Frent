#!/bin/bash

echo "This script will initialize docfx under npm-watch to watch for any changes.
This is going to install npm-watch, which requires node and npm.
Continue? (y/N)"

read -n 1 -r response
echo ""

if [[ ! "$response" =~ ^[Yy]$ ]]; then
    echo "Operation canceled by the user."
    exit 0
fi

if [ ! -f "package.json" ]; then
    echo "\"package.json\" not found. Initializing new node project..."
    
    cat <<'EOF' > package.json
{
    "name": "docs",
    "version": "1.0.0",
    "private": true,
    "scripts": {
        "serve": "dotnet docfx docfx.json --serve",
        "watch": "npm-watch serve"
    },
    "watch": {
        "serve": {
            "patterns": [
                ".",
                "docs"
            ],
            "extensions": "md,yml",
            "ignore": [
                "_site",
                "_site/**",
                "api",
                "api/**",
                ".git",
                "node_modules"
            ],
            "runAtStart": true,
            "quiet": false,
            "delay": "500"
        }
    },
    "devDependencies": {
        "npm-watch": "^0.13.0"
    }
}
EOF

fi

echo "Initializing docfx's server under npm-watch"

echo "Running \"npm install\"..."
npm install

echo "Starting npm-watch... (saving a file will serve the page again!)"
npm run watch
