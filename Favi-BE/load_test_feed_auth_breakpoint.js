import http from 'k6/http';
import { check, sleep } from 'k6';

// Kịch bản: Breakpoint Testing cho User đã login tải feed
export const options = {
    // Tăng dần lượng User lớn từ từ không nghỉ, cho đến khi Server lỗi
    stages: [
        { duration: '30s', target: 10 },   // Khởi động nhẹ
        { duration: '1m', target: 50 },    // Tăng từ 10 lên 50 VUs
        { duration: '1m', target: 150 },   // Tăng từ 50 lên 150 VUs
        { duration: '1m', target: 250 },   // Tăng max lên 250 VUs
    ],
    thresholds: {
        // Tự động dừng test nếu tỉ lệ request lỗi vượt quá 5% (trì hoãn kiểm tra 1 phút đầu)
        http_req_failed: [{ threshold: 'rate<0.05', abortOnFail: true, delayAbortEval: '1m' }],
        // Tự động dừng test nếu thời gian phản hồi ở 90% lượng request vượt quá 5s (trì hoãn 1 phút đầu)
        http_req_duration: [{ threshold: 'p(90)<5000', abortOnFail: true, delayAbortEval: '1m' }],
    },
};

const testUsers = [
    { email: 'alex_photo@example.com' },
    { email: 'sarah_art@example.com' },
    { email: 'mike_travel@example.com' },
    { email: 'emma_food@example.com' },
    { email: 'david_tech@example.com' },
    { email: 'lisa_fitness@example.com' },
    { email: 'john_music@example.com' },
    { email: 'anna_style@example.com' },
    { email: 'chris_gaming@example.com' },
    { email: 'nina_books@example.com' },
];

// Setup chạy 1 lần duy nhất trước khi bắt đầu VUs
export function setup() {
    const baseUrl = 'http://localhost:5000/api';
    const tokens = [];

    // Login để lấy trước token, tránh load test đè lên login API
    for (const user of testUsers) {
        const loginPayload = JSON.stringify({
            EmailOrUsername: user.email,
            Password: 'Password123!'
        });

        const loginRes = http.post(`${baseUrl}/auth/login`, loginPayload, {
            headers: { 'Content-Type': 'application/json' },
        });

        if (loginRes.status === 200) {
            const token = loginRes.json('accessToken') || loginRes.json('token');
            if (token) tokens.push(token);
        }
    }

    return { tokens };
}

export default function (data) {
    const baseUrl = 'http://localhost:5000/api';

    if (!data.tokens || data.tokens.length === 0) {
        return; 
    }

    // Lấy random token đã được tạo từ hàm setup()
    const token = data.tokens[Math.floor(Math.random() * data.tokens.length)];
    const feedUrl = `${baseUrl}/posts/feed-with-reposts?page=1&pageSize=20`;

    const feedRes = http.get(feedUrl, {
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });

    check(feedRes, {
        'auth fetch feed successful (status 200)': (r) => r.status === 200,
    });

    sleep(1);
}
