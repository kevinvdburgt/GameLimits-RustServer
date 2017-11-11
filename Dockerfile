FROM node:8

# Create the app directory
RUN mkdir -p /app
WORKDIR /app

# Install the app dependencies
COPY package.json /app/
COPY package-lock.json /app/
RUN npm install --production

# Bundle app source
COPY . /app

# Expose the app port
EXPOSE 7777

# Starting the app
ADD ./entrypoint.sh /
RUN chmod +x /entrypoint.sh
CMD ["/entrypoint.sh"]
