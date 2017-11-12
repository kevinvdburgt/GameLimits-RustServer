#!/bin/sh

/wait.sh mysql:3306

cd /app

# rm -rf node_modules
# npm install

npm start
