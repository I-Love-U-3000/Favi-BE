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
    { email: 'user_00001@seed.local', password: '123456' },
    { email: 'user_00002@seed.local', password: '123456' },
    { email: 'user_00003@seed.local', password: '123456' },
    { email: 'user_00004@seed.local', password: '123456' },
    { email: 'user_00005@seed.local', password: '123456' },
    { email: 'user_00006@seed.local', password: '123456' },
    { email: 'user_00007@seed.local', password: '123456' },
    { email: 'user_00008@seed.local', password: '123456' },
    { email: 'user_00009@seed.local', password: '123456' },
    { email: 'user_00010@seed.local', password: '123456' },
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
        Password: randomUser.password
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
