# Instaswarm
![GPL License](https://img.shields.io/badge/License-GPLv3-blue.svg)
**are you wasting too much time scrolling reels well this tool is for you turn your addiction to an addiction into a addiction with slight benefits of getting views online, share funny videos to this bot and build up an creator account completely hands free, no more friends telling you to stop sending your entire FYP. improve for better**

**InstaSwarm is a .NET-based Docker container application that automates posting Instagram Reels across multiple accounts**. Share reels with the admin account via Instagram, and the bot downloads, processes, and uploads them to managed accounts, helping creators grow their audience hands-free.

## Features
- Manages multiple Instagram accounts from a single admin account.
- Processes Instagram webhooks to handle reel uploads.
- Dockerized for easy deployment on Linux servers.
- Uses Tailscale for secure endpoint and video directory access.

## Prerequisites
- Linux server
- [Tailscale VPN](https://tailscale.com/)
- Git
- Docker Compose
- Meta Developer account for Instagram API access

## Install
1. Clone the repository: 
	```bash
	git clone https://github.com/demetrebadzaradze/InstaSwarm.git
	``` 
2. create directory for videos, this must be outside of the project directory:
	```bash
	mkdir -p ~/opt/video
	sudo chown 1000 ~/opt/video
	chmod -R 777 ~/opt/video
	```
3. Follow the Setup Guides for Tailscale [[Instaswarm#Tailscale Guide]], Meta app [[Instaswarm#Meta app Guide]], and .env configuration [[Instaswarm#.ENV guide]].

## Running the app
app does comes with `compose.yaml` file you can either run that:
```bash
docker compose up -d
```
 or run with Dockerfile directly:
 - build
	```bash
	sudo docker build -t instaswarm .
	```
 - run
	```bash
	sudo docker run --rm --env-file .env -v /home/<username>/opt/video:/app/video -p 5000:8080 -p 5001:8081 --name Instaswarm instaswarm
	```
you can of course tweak this.

## Usage
	 
Send a reel to the admin Instagram account(one that has webhooks configured) via direct message. The bot will download, process, and upload the reel to all managed accounts.

### Tailscale Guide
1. download it from [here](https://tailscale.com/download/linux) and go thru setup process.
2. enable funnel. learn [here.](https://tailscale.com/kb/1223/funnel) 
3. and funnel the needed videos directory path
	```bash
	tailscale funnel --bg "~/opt/video"
	```
	and also the port where app is living (HTTPS port)
	```bash
	tailscale funnel --bg --https 10000 https+insecure://127.0.0.1:5001
	```
	
### Meta app Guide
1. for this go over to [Facebook for developer website](https://developers.facebook.com/) and sign up.
2. make and [app](https://developers.facebook.com/apps/)
	- name it and enter your E-mal.
	- is use case chouse `other`.
	- app type `business`.
	- then add product `Instagram`
	- go over to the `API Setup with Instagram login`
	- and add an account or accounts that will be posting from App roles -> Roles -> Add people. and sent a invitation as `Instagram tester` with username (account needs to be converted as creator). then on that account accept invitation from profile -> gear icon -> apps and websites -> tester Invites and accept
	- now generate access token on `Instagram` -> `API Setup with Instagram login` page click on generate token on each account and save them for latter. (tip: sometimes it will give error after login if account is recently added so wait for like a hour or two and then retry)
	- also whichever account you will be using as admin account( meaning messaging reels) enable webhooks for that account 
	- for the configure webhooks as callback URL enter whatever Tailscale funnel gave you (Link) and add `/webhook/instagram` at the end.
	- for `verify token` this is like a password for webhook for verification so enter something save and save that for later too.
	- also here subscribe to messages.
	- and after app is running hit `verify and save` (at tis point this is not ready).
- also set the `App Mode` to `Live`

## .ENV guide
in this project environmental variables are most important thing for app to work. `.env.example` is the example file and it will look something like this(with small descriptions):
```env
INSTAGRAM_USER_TOKENS=IGJHHTDMHNBVMHY, more...	 
YTDLP_PATH=
VIDEO_DOWNLOAD_PATH=./video
HTTPS_CERT_PASSWORD=Instaswarm12345
PUBLIC_BASE_URL=https://test.taile6d42d.ts.net/
WEBHOOKK_VERIFY_TOKEN=VERIFY_TOKEN
ADMIN_INSTAGRAM_USER_ID=12456789
VIDEO_DOWNLOAD_PATH_ON_HOST=C:\Users\TG\Pictures\share
```
1. `INSTAGRAM_USER_TOKEN` is Instagram users token from meta app dashboard and it is used for fetching data about user and also for uploading on their profile. in the future there will be many like this for clustering like `INSTAGRAM_USER_TOKEN_1`, `INSTAGRAM_USER_TOKEN_2` or `INSTAGRAM_USER_TOKEN_ADMIN` and such.
2. `YTDLP_PATH` this is the path where your `Yt-dlp` tool executable is located at. since container OS is Linux and `.exe` don't work once Dockerfile downloads the tool it also makes a link from its binary and exe file in the app directory: 
	```bash
	ln -s /usr/local/bin/yt-dlp /app/yt-dlp.exe
	```
	so no need for `yt-dlp` path if this is not edited and everything is 'stock'.
3. `VIDEO_DOWNLOAD_PATH` is a path where the apps will be downloaded in. by default this is set to the `./video` folder inside the `app` directory and is also  a volume for Tailscale to host on host machine set this as default too but the Tailscale could be moved into a container in the future. if you need to change the destination path you will also need to edit volumes and folder creation in the Dockerfile.
4. `HTTPS_CERT_PASSWORD` is a password for your HTTPS certificate, if you didn't make made your own then just use included with password "Instaswarm12345".
5. `PUBLIC_BASE_URL` is from Tailscale so when you funnel the app what URL is it accessible on (Eg.`https://test.taile6d42d.ts.net/`) so a 
	```bash
	HTTPS://{MACHINE_NAME}.{TAILNET_NAME}.ts.ent/
	``` 
	**it must have a slash at the end**
	this could be fount in Tailscale admin console. also for testing you could just funnel something and it will output URL like this and end the funnel. 
6. `WEBHOOKK_VERIFY_TOKEN` is a token that is used for verifying the webhook. so once you set it in the `.env` file it also will be used in meta dashboard.
7. `ADMIN_INSTAGRAM_USER_ID` is and id of an user where app accepts messages from, at first you can set this to `2123123` or something and then send the message to account and check the webhook in the logs with:
	```bash
	docker logs <container name>
	```
8. `VIDEO_DOWNLOAD_PATH_ON_HOST` is where will the videos be saved temporally on host machine, this should be same that was funneled with Tailscale and this also must be an absolute path. 

## Make your own HTTPS certificate (optional but recommended)
according to [this](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs)  dotnet can make the https certificate but if you don't have it run a container that app uses to run like this:
```bash
docker run --rm -v "~/opt/certs:/output" mcr.microsoft.com/dotnet/sdk:8.0 bash -c "
  dotnet dev-certs https --export-path /output/https-dev.pfx --password '<your strong password>' &&
  chown 1000:1000 /output/https-dev.pfx &&
  chmod 644 /output/https-dev.pfx
"
```
replace the password and after running this it will make HTTPS certificate in ~/opt/certs directory.
after that move that certificate to the app redirect and enter password in `.env` file 


## License
This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](https://github.com/demetrebadzaradze/InstaSwarm/blob/master/LICENSE) file for details.
