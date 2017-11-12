FROM node:8

# Create the app directory
RUN mkdir -p /app
WORKDIR /app

# Install the app dependencies
COPY package.json /app/
COPY package-lock.json /app/
RUN npm install

# Bundle app source
COPY . /app

# Expose the app port
EXPOSE 7777

# Starting the app
ADD ./entrypoint.sh ./wait.sh /
RUN chmod +x /entrypoint.sh /wait.sh
CMD ["/entrypoint.sh"]
