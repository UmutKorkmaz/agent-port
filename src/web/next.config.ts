import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: "/api/platform/:path*",
        destination: "http://localhost:5001/api/v1/:path*",
      },
      {
        source: "/api/ai/:path*",
        destination: "http://localhost:5002/:path*",
      },
    ];
  },
};

export default nextConfig;
