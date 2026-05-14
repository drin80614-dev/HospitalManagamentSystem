const ORIGIN = "https://hospitalmanagamentsystem.onrender.com";
const ORIGIN_HOST = "hospitalmanagamentsystem.onrender.com";

function rewriteOriginUrl(value, publicOrigin) {
  if (!value) {
    return value;
  }

  try {
    const url = new URL(value);
    if (url.hostname === ORIGIN_HOST) {
      return `${publicOrigin}${url.pathname}${url.search}${url.hash}`;
    }
  } catch {
    // Relative URLs are already safe for the public frontend origin.
  }

  return value
    .replace(`https://${ORIGIN_HOST}`, publicOrigin)
    .replace(`http://${ORIGIN_HOST}`, publicOrigin);
}

function renderWarmupPage(publicOrigin) {
  const html = `<!doctype html>
<html lang="sq">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <meta http-equiv="refresh" content="8" />
  <title>Vlera Dent - Duke u hapur</title>
  <style>
    :root { color-scheme: light; font-family: Inter, ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif; }
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: radial-gradient(circle at top left, #e0f7ff, transparent 34%), linear-gradient(135deg, #f8fdff, #ecf8ff); color: #14213d; }
    .card { width: min(92vw, 560px); padding: 36px; border: 1px solid rgba(14, 151, 210, .18); border-radius: 28px; background: rgba(255,255,255,.88); box-shadow: 0 24px 80px rgba(5, 82, 118, .14); }
    .brand { display: flex; align-items: center; gap: 14px; margin-bottom: 26px; }
    .logo { width: 58px; height: 58px; border-radius: 18px; display: grid; place-items: center; background: linear-gradient(135deg, #0797d2, #2f3294); color: white; font-weight: 900; font-size: 24px; }
    h1 { margin: 0 0 10px; font-size: clamp(28px, 5vw, 42px); line-height: 1.05; }
    p { margin: 0; color: #64748b; font-size: 17px; line-height: 1.55; }
    .actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 28px; }
    a { border-radius: 14px; padding: 12px 18px; text-decoration: none; font-weight: 800; }
    .primary { background: #0797d2; color: white; }
    .secondary { color: #2f3294; background: #edf7ff; }
    .pulse { width: 10px; height: 10px; border-radius: 999px; background: #22c55e; box-shadow: 0 0 0 0 rgba(34,197,94,.45); animation: pulse 1.5s infinite; }
    @keyframes pulse { 70% { box-shadow: 0 0 0 14px rgba(34,197,94,0); } 100% { box-shadow: 0 0 0 0 rgba(34,197,94,0); } }
  </style>
</head>
<body>
  <main class="card">
    <div class="brand">
      <div class="logo">VD</div>
      <div>
        <strong>Vlera Dent</strong>
        <p style="font-size:14px">Dental Clinic OS</p>
      </div>
      <span class="pulse" aria-hidden="true"></span>
    </div>
    <h1>Aplikacioni po zgjohet</h1>
    <p>Render mund te vonohet disa sekonda pas pauzes. Faqja rifreskohet automatikisht dhe nuk ka humbje te te dhenave.</p>
    <div class="actions">
      <a class="primary" href="${publicOrigin}/Auth/Login">Provo perseri</a>
      <a class="secondary" href="${publicOrigin}/healthz">Kontrollo statusin</a>
    </div>
  </main>
</body>
</html>`;

  return new Response(html, {
    status: 200,
    headers: {
      "Content-Type": "text/html; charset=utf-8",
      "Cache-Control": "no-store, no-cache, must-revalidate, max-age=0",
      "X-Vlera-Dent-Frontend": "cloudflare-warmup"
    }
  });
}

function createOriginRequest(request) {
  const incomingUrl = new URL(request.url);
  const originUrl = new URL(request.url);
  originUrl.protocol = "https:";
  originUrl.hostname = ORIGIN_HOST;
  originUrl.port = "";

  const headers = new Headers(request.headers);
  headers.set("X-Forwarded-Host", incomingUrl.host);
  headers.set("X-Forwarded-Proto", "https");
  headers.set("X-Forwarded-Origin", incomingUrl.origin);

  const init = {
    method: request.method,
    headers,
    redirect: "manual",
  };

  if (request.method !== "GET" && request.method !== "HEAD") {
    init.body = request.body;
  }

  return new Request(originUrl.toString(), init);
}

function rewriteResponseHeaders(response, publicOrigin) {
  const headers = new Headers(response.headers);
  const location = headers.get("Location");

  if (location) {
    headers.set("Location", rewriteOriginUrl(location, publicOrigin));
  }

  headers.delete("server");
  headers.set("X-Vlera-Dent-Frontend", "cloudflare");

  return headers;
}

export default {
  async fetch(request) {
    const publicOrigin = new URL(request.url).origin;
    const originRequest = createOriginRequest(request);
    let response;

    try {
      response = await fetch(originRequest);
    } catch {
      return renderWarmupPage(publicOrigin);
    }

    if ([502, 503, 504].includes(response.status)) {
      return renderWarmupPage(publicOrigin);
    }

    const headers = rewriteResponseHeaders(response, publicOrigin);

    return new Response(response.body, {
      status: response.status,
      statusText: response.statusText,
      headers,
    });
  },
};
