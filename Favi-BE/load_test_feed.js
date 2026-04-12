import http from 'k6/http';
import { check, sleep } from 'k6';

// Kịch bản load test: Tăng tải từ từ -> Giữ tải -> Spike
export const options = {
    stages: [
        { duration: '10s', target: 10 },  // Ramping phase
        { duration: '20s', target: 10 },  // Constant phase
        { duration: '5s', target: 50 },   // Spike phase
        { duration: '10s', target: 50 },
        { duration: '10s', target: 0 },   // Cool down phase
    ]
};

// Hàm hỗ trợ để lấy token ngẫu nhiên (hoặc bạn có thể dùng testUsers như ở load_test_login.js rồi login để lấy token từng VUs)
// Để đơn giản, giả sử user có sẵn JWT token hoặc sẽ Login -> lấy Token -> gọi API Feed.
// Dưới đây là cách mô phỏng gọi API Login ĐỂ LẤY TOKEN trước khi test feed

const testUsers = [
    { email: 'alex_photo@example.com' },
    { email: 'sarah_art@example.com' },
    { email: 'mike_travel@example.com' },
    { email: 'emma_food@example.com' },
    { email: 'david_tech@example.com' }
];

export default function () {
    const baseUrl = 'http://localhost:5000/api';
    
    // 1. Phải Login để lấy Token vì API feed yêu cầu auth
    const loginUrl = `${baseUrl}/auth/login`;
    const randomUser = testUsers[Math.floor(Math.random() * testUsers.length)];
    
    const loginPayload = JSON.stringify({
        EmailOrUsername: randomUser.email,
        Password: 'Password123!'
    });

    const loginRes = http.post(loginUrl, loginPayload, {
        headers: { 'Content-Type': 'application/json' },
    });

    check(loginRes, {
        'login successful': (r) => r.status === 200,
    });

    if (loginRes.status !== 200) {
        return; // Nếu login tạch thì nghỉ test feed user này
    }

    const token = loginRes.json('accessToken') || loginRes.json('token');

    // 2. Test Get Personal Feed (có Reposts)
    const feedUrl = `${baseUrl}/posts/feed-with-reposts?page=1&pageSize=20`;
    
    const feedRes = http.get(feedUrl, {
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    });

    check(feedRes, {
        'fetch feed successful (status 200)': (r) => r.status === 200,
        'body size > 0': (r) => r.body.length > 0,
    });

    sleep(1);
}
