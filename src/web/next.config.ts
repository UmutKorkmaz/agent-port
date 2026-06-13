import type { NextConfig } from "next";

// Proxy targets are env-driven so the same build runs both locally (defaults to
// localhost) and inside the prod compose network (set to the compose service
// hostnames, e.g. http://platform-api:5001 and http://ai-services:5002).
const platformApiOrigin =
  process.env.PLATFORM_API_ORIGIN ?? "http://localhost:5001";
const aiServicesOrigin =
  process.env.AI_SERVICES_ORIGIN ?? "http://localhost:5002";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  // Emit a self-contained server bundle so the runtime image can ship without
  // node_modules or the full source tree.
  output: "standalone",
  async rewrites() {
    return [
      {
        source: "/api/platform/:path*",
        destination: `${platformApiOrigin}/api/v1/:path*`,
      },
      {
        source: "/api/ai/:path*",
        destination: `${aiServicesOrigin}/:path*`,
      },
    ];
  },
};

export default nextConfig;
