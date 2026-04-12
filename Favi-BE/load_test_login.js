import http from 'k6/http';
import { check, sleep } from 'k6';

// Định nghĩa kịch bản: Ramping -> Constant -> Spike 
export const options = {
    stages: [
        // 1. Ramping phase: Từ từ tăng lên 20 Users ảo trong 15 giây
        { duration: '15s', target: 20 },
        // 2. Constant phase: Giữ tải đều đặn 20 Users trong 30 giây
        { duration: '30s', target: 20 },
        // 3. Spike phase: Đột ngột đẩy tải lên lượng lớn (ví dụ 100 Users) trong 5 giây
        { duration: '5s', target: 100 },
        // Giữ tải spike trong 10 giây xem server chịu được không
        { duration: '10s', target: 100 },
        // 4. Cool down phase: Từ từ giảm tải về 0 trong 10 giây
        { duration: '10s', target: 0 },
    ]
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
    { email: 'tom_films@example.com' },
    { email: 'kate_science@example.com' },
    { email: 'steve_cars@example.com' },
    { email: 'amy_design@example.com' },
    { email: 'pete_sports@example.com' },
    { email: 'julia_coffee@example.com' },
    { email: 'mark_pizza@example.com' },
    { email: 'rachel_comedy@example.com' },
    { email: 'dan_history@example.com' },
    { email: 'zoe_nature@example.com' }
];

export default function () {
    // Lưu ý thay đổi port phù hợp (thường xem ở Properties/launchSettings.json)
    // Ví dụ API chạy ở http://localhost:5000
    const url = 'http://localhost:5000/api/auth/login';

    // Chọn ngẫu nhiên 1 user từ danh sách seed data
    const randomUser = testUsers[Math.floor(Math.random() * testUsers.length)];

    // Dựa theo AuthController
    // record LoginDto(string EmailOrUsername, string Password);
    const payload = JSON.stringify({
        EmailOrUsername: randomUser.email,
        Password: 'Password123!' // Mật khẩu chung đã ghi ở cuối file accounts.txt
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);

    check(res, {
        'login successful (status 200)': (r) => r.status === 200,
    });

    sleep(1);
}
