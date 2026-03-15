# HTTPS Setup For Universal Links

This setup keeps the existing service containers unchanged and adds TLS only at the Nginx edge.

## What Was Added

- `deployment/nginx/nginx.conf`
  Adds `/.well-known/acme-challenge/` support so Certbot webroot validation can work on the current HTTP stack.
- `deployment/nginx/nginx.https.conf`
  Adds HTTP-to-HTTPS redirect and TLS virtual hosts for:
  - `go-nomads.com`
  - `www.go-nomads.com`
  - `api.go-nomads.com`
- `docker-compose-services-swr-https.yml`
  Starts the TLS-enabled Nginx container and mounts Let's Encrypt certificates.

## Prerequisites

- DNS for `go-nomads.com`, `www.go-nomads.com`, `api.go-nomads.com` points to this server.
- Ports `80` and `443` are open.
- Certbot is installed on the host.

## 1. Prepare ACME Webroot

```bash
cd /path/to/go-nomads-backend
mkdir -p deployment/nginx/certbot-www
```

## 2. Start The Current HTTP Stack First

Use the existing compose file so the ACME challenge can be served over HTTP:

```bash
docker compose -f docker-compose-services-swr.yml up -d
```

## 2.5. Verify The ACME Path Before Running Certbot

Create a probe file in the webroot:

```bash
cd /path/to/go-nomads-backend
mkdir -p deployment/nginx/certbot-www/.well-known/acme-challenge
echo ok > deployment/nginx/certbot-www/.well-known/acme-challenge/probe.txt
docker compose -f docker-compose-services-swr.yml restart nginx
```

Now verify all domains over plain HTTP:

```bash
curl -i http://go-nomads.com/.well-known/acme-challenge/probe.txt
curl -i http://www.go-nomads.com/.well-known/acme-challenge/probe.txt
curl -i http://api.go-nomads.com/.well-known/acme-challenge/probe.txt
```

Expected result for all three: `HTTP/1.1 200 OK` and body `ok`.

If you get `404`, Nginx is not seeing the webroot files yet.

If you get `401`, `403`, or a JSON/API response, traffic is still not reaching this Nginx location block first. That usually means one of these is true:

- DNS points the domain to a different server.
- A cloud WAF/CDN or another reverse proxy is handling the request before this host.
- The currently running Nginx container has not been recreated with the latest config and volume mounts.

Do not run Certbot until the probe file is reachable with `200 OK` on every domain you want in the certificate.

## 3. Issue Certificates With Certbot

```bash
sudo certbot certonly \
  --webroot \
  -w /absolute/path/to/go-nomads-backend/deployment/nginx/certbot-www \
  -d go-nomads.com \
  -d www.go-nomads.com \
  -d api.go-nomads.com
```

Notes:

- This command creates one SAN certificate covering all three names and stores it under `/etc/letsencrypt/live/go-nomads.com/`.
- `api.go-nomads.com` uses the same certificate files because it is included in that SAN certificate.

## 4. Switch To TLS Nginx

After certificates are issued:

```bash
docker compose -f docker-compose-services-swr.yml stop nginx
docker compose -f docker-compose-services-swr-https.yml up -d
```

Make sure the separate web stack is still running on the shared Docker network before switching traffic:

```bash
cd /path/to/go-nomads-web
docker compose -f docker-compose-web-swr.yml up -d
```

Also make sure backend containers such as `go-nomads-gateway` are already running before starting the standalone HTTPS nginx compose, because this file only defines the edge proxy.

If the service stack is already up, you only need to replace the old HTTP-only `nginx` container with the TLS-enabled one.

## 5. Verify

```bash
curl -I https://go-nomads.com
curl -I https://api.go-nomads.com
curl -I https://go-nomads.com/.well-known/apple-app-site-association
```

Expected results:

- HTTPS responds normally.
- `apple-app-site-association` is reachable over HTTPS.
- iOS Universal Links can now pass Apple, WeChat, and QQ validation prerequisites.

## Renewal

Let's Encrypt certificates need renewal. Typical host cron:

```bash
sudo certbot renew
docker restart go-nomads-nginx
```

## Important

- `app_links` / AASA validation only cares that the public endpoint is HTTPS.
- Your upstream services can remain HTTP inside Docker.
- Do not commit real certificate files into the repository.
