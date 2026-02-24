# ITU-MiniTwit

guide to provision a VM and deploy ITU-MiniTwit (web app + simulator API).

## 1. Clone

```bash
git clone git@github.com:sebseb10/ITU-MiniTwit.git
cd ITU-MiniTwit
```

## 2. Prerequisites

- DigitalOcean account
- DigitalOcean API token (write access)
- Your SSH public key added in DigitalOcean (`API` -> `SSH Keys`)
- Local tools: `bash`, `curl`, `ssh`, `rsync`

## 3. Set secrets locally in terminal. 
```bash
export DO_TOKEN="<digitalocean_api_token>"
export DO_SSH_KEY_FINGERPRINT="<digitalocean_ssh_key_fingerprint>"
- Do not commit `DO_TOKEN` or any other secrets.
```

## 4. Provision VM (Task 2a)

```bash
./scripts/provision_itu_minitwit.sh
```

The script prints `DROPLET_IP=<ip>`.

## 5. Deploy release to VM (Task 2b)

```bash
export DEPLOY_HOST="<DROPLET_IP>"
./scripts/deploy_itu_minitwit.sh
```

## 6. Verify deployment

Open in browser:

- App: `<DROPLET_IP>:5001/`
- API: `<DROPLET_IP>/latest` // Unsure if this is implemetned correctly / what is expected?. 


current website : 
- App: `http://64.225.111.92:5001/`
- API: `http://64.225.111.92:5001/latest`
Quick API check to check website Latest endpoint is up:
```bash
curl -i http://64.225.111.92:5001/latest
```
