import { QRCodeSVG } from "qrcode.react";

interface QrPageProps {
    expired?: boolean;
}

export default function QrPage({ expired = false }: QrPageProps) {
    // Thêm ?source=qr để backend ghi nhận lượt truy cập (UC01.3, BR-20)
    const entryUrl = new URL("/", window.location.origin);
    entryUrl.searchParams.set("source", "qr");

    const websiteUrl = entryUrl.toString();

    const isLocal =
        window.location.hostname === "localhost" ||
        window.location.hostname === "127.0.0.1";

    return (
        <main
            style={{
                minHeight: "80vh",
                display: "grid",
                placeItems: "center",
                padding: "32px 16px",
                background: "#f0f7f2",
            }}
        >
            <section
                style={{
                    width: "100%",
                    maxWidth: 560,
                    padding: "36px 24px",
                    textAlign: "center",
                    borderRadius: 28,
                    background: "#ffffff",
                    boxShadow: "0 16px 48px #163e2c14",
                    color: "#163e2c",
                }}
            >
                <p style={{ color: "#168652", fontWeight: 700 }}>
                    🌿 THẢO CẦM VIÊN
                </p>

                {expired && (
                    <div
                        role="alert"
                        style={{
                            margin: "12px 0 18px",
                            padding: "14px 16px",
                            borderRadius: 14,
                            background: "#fdecec",
                            color: "#b42318",
                            fontWeight: 600,
                            lineHeight: 1.5,
                        }}
                    >
                        ⏱️ Phiên tham quan (1 phút) đã hết hạn. Vui lòng quét lại mã QR để tiếp tục.
                    </div>
                )}

                <h1>Quét mã để khám phá</h1>

                <p style={{ color: "#617268", lineHeight: 1.7 }}>
                    Mở camera điện thoại, quét mã QR để truy cập bản đồ
                    và nghe thuyết minh đa ngôn ngữ.
                </p>

                {isLocal ? (
                    <p
                        role="alert"
                        style={{
                            padding: 20,
                            borderRadius: 16,
                            background: "#fff3cd",
                            color: "#795700",
                            lineHeight: 1.7,
                        }}
                    >
                        Bạn đang mở bằng localhost. Hãy mở đường dẫn Cloudflare
                        và thêm /qr ở cuối để hiển thị mã QR dùng cho điện thoại.
                        <br />
                        <a href={websiteUrl} style={{ color: "#168652", fontWeight: 700 }}>
                            Hoặc bấm vào đây để giả lập quét QR trên máy tính
                        </a>
                    </p>
                ) : (
                    <>
                        <div
                            style={{
                                display: "inline-block",
                                maxWidth: "100%",
                                margin: "20px 0",
                                padding: 16,
                                background: "#fff",
                                border: "2px solid #168652",
                                borderRadius: 20,
                            }}
                        >
                            <QRCodeSVG
                                value={websiteUrl}
                                size={280}
                                level="M"
                                marginSize={4}
                                title="Truy cập website Thảo Cầm Viên"
                                style={{
                                    display: "block",
                                    maxWidth: "100%",
                                    height: "auto",
                                }}
                            />
                        </div>

                        <p>
                            <a
                                href={websiteUrl}
                                style={{ color: "#168652", overflowWrap: "anywhere" }}
                            >
                                {websiteUrl}
                            </a>
                        </p>
                    </>
                )}

                <p style={{ color: "#67458b", fontWeight: 600 }}>
                    Việt · Anh · Trung · Hàn · Nhật
                </p>
            </section>
        </main>
    );
}