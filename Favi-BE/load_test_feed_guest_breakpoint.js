import http from 'k6/http';
import { check, sleep } from 'k6';

// Kịch bản: Breakpoint Testing cho Guest tải feed
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

export default function () {
    const baseUrl = 'http://localhost:5000/api';
    const feedUrl = `${baseUrl}/posts/guest-feed?page=1&pageSize=20`;

    const feedRes = http.get(feedUrl, {
        headers: {
            'Content-Type': 'application/json'
        }
    });

    check(feedRes, {
        'guest fetch feed successful (status 200)': (r) => r.status === 200,
    });

    sleep(1);
}
