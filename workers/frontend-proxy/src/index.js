const ORIGIN = "https://hospitalmanagamentsystem.onrender.com";
const ORIGIN_HOST = "hospitalmanagamentsystem.onrender.com";

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
    headers.set("Location", location.replace(ORIGIN, publicOrigin));
  }

  headers.delete("server");
  headers.set("X-Vlera-Dent-Frontend", "cloudflare");

  return headers;
}

export default {
  async fetch(request) {
    const publicOrigin = new URL(request.url).origin;
    const originRequest = createOriginRequest(request);
    const response = await fetch(originRequest);
    const headers = rewriteResponseHeaders(response, publicOrigin);

    return new Response(response.body, {
      status: response.status,
      statusText: response.statusText,
      headers,
    });
  },
};
