import http from "k6/http";
import { check, group, sleep } from "k6";

const baseUrl = (__ENV.API_BASE_URL || "http://api:8080").replace(/\/+$/, "");

export const options = {
  vus: Number(__ENV.K6_VUS || 10),
  duration: __ENV.K6_DURATION || "30s",
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<1000"],
    checks: ["rate>0.99"]
  }
};

export default function () {
  group("API health", () => {
    const response = http.get(`${baseUrl}/api/test/health`);

    check(response, {
      "health status is 200": (r) => r.status === 200,
      "health response is healthy": (r) => {
        try {
          return r.json("status") === "healthy";
        } catch {
          return false;
        }
      }
    });
  });

  group("Courses list", () => {
    const response = http.get(`${baseUrl}/api/courses?pageNumber=1&pageSize=10`);

    check(response, {
      "courses status is 200": (r) => r.status === 200,
      "courses response has items": (r) => {
        try {
          return Array.isArray(r.json("items"));
        } catch {
          return false;
        }
      }
    });
  });

  group("Categories list", () => {
    const response = http.get(`${baseUrl}/api/courses/categories`);

    check(response, {
      "categories status is 200": (r) => r.status === 200,
      "categories response is array": (r) => {
        try {
          return Array.isArray(r.json());
        } catch {
          return false;
        }
      }
    });
  });

  sleep(1);
}
