import {
    useCallback,
    useEffect,
    useState,
    type ChangeEvent,
    type FormEvent,
} from "react";

import {
    placeApi,
    savePlaceDraft,
} from "../../services/placeApi";

import { ApiError } from "../../services/api";
import type { Place } from "../../types/place";

const MAP_WIDTH = 1024;
const MAP_HEIGHT = 643;

const MAX_IMAGE_SIZE = 5 * 1024 * 1024;

const IMAGE_TYPES = [
    "image/jpeg",
    "image/png",
    "image/webp",
];

const CATEGORY_LABELS: Record<Place["category"], string> = {
    animal: "🐾 Động vật",
    plant: "🌳 Thực vật",
    facility: "📍 Tiện ích",
};

type NumberField =
    | "latitude"
    | "longitude"
    | "radiusMeters"
    | "mapX"
    | "mapY"
    | "priority";

interface DraftForm {
    id?: string;
    code?: string;

    nameVi: string;
    descriptionVi: string;
    category: Place["category"];

    latitude: string;
    longitude: string;
    radiusMeters: string;

    mapX: string;
    mapY: string;
    priority: string;

    currentImageUrl: string | null;
}

function createEmptyForm(): DraftForm {
    return {
        nameVi: "",
        descriptionVi: "",
        category: "animal",

        latitude: "",
        longitude: "",
        radiusMeters: "30",

        mapX: String(MAP_WIDTH / 2),
        mapY: String(MAP_HEIGHT / 2),
        priority: "1",

        currentImageUrl: null,
    };
}

function getErrorMessage(
    error: unknown,
    fallback: string,
): string {
    if (!(error instanceof ApiError)) {
        return fallback;
    }

    switch (error.status) {
        case 401:
            return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";

        case 403:
            return "Yêu cầu bị từ chối. Vui lòng kiểm tra quyền truy cập.";

        case 404:
        case 405:
            return "API chưa hỗ trợ thao tác này hoặc không tìm thấy dữ liệu.";

        case 413:
            return "Ảnh vượt quá kích thước máy chủ cho phép.";

        default:
            return error.message || fallback;
    }
}

export default function SpotsAdmin() {
    const [places, setPlaces] = useState<Place[]>([]);
    const [loading, setLoading] = useState(true);
    const [listError, setListError] = useState<string | null>(null);

    const [form, setForm] = useState<DraftForm | null>(null);
    const [imageFile, setImageFile] = useState<File | null>(null);
    const [previewUrl, setPreviewUrl] = useState<string | null>(null);

    const [saving, setSaving] = useState(false);
    const [formError, setFormError] = useState<string | null>(null);
    const [notice, setNotice] = useState<string | null>(null);

    const [hideTarget, setHideTarget] = useState<Place | null>(null);
    const [hiding, setHiding] = useState(false);
    const [hideError, setHideError] = useState<string | null>(null);

    const loadPlaces = useCallback(async () => {
        setLoading(true);
        setListError(null);

        try {
            const result = await placeApi.adminList();
            setPlaces(result);
        } catch (error) {
            setListError(
                getErrorMessage(
                    error,
                    "Không tải được danh sách điểm tham quan.",
                ),
            );
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadPlaces();
    }, [loadPlaces]);

    // Tạo URL xem trước và giải phóng khi đổi hoặc đóng ảnh.
    useEffect(() => {
        if (!imageFile) {
            setPreviewUrl(null);
            return;
        }

        const url = URL.createObjectURL(imageFile);
        setPreviewUrl(url);

        return () => {
            URL.revokeObjectURL(url);
        };
    }, [imageFile]);

    function openCreate() {
        setNotice(null);
        setFormError(null);
        setImageFile(null);
        setForm(createEmptyForm());
    }

    function openEdit(place: Place) {
        setNotice(null);
        setFormError(null);
        setImageFile(null);

        setForm({
            id: place.id,
            code: place.slug,

            // Backend hiện dùng mã khu làm tên dự phòng.
            // Không coi mã dự phòng là tên tiếng Việt đã nhập.
            nameVi:
                place.name?.vi === place.slug
                    ? ""
                    : place.name?.vi ?? "",

            descriptionVi: place.desc?.vi ?? "",
            category: place.category,

            latitude: String(place.lat),
            longitude: String(place.lng),
            radiusMeters: String(place.radiusMeters),

            mapX: String(place.mapX),
            mapY: String(place.mapY),
            priority: String(place.priority),

            currentImageUrl: place.imageUrl,
        });
    }

    function closeForm() {
        if (saving) return;

        setForm(null);
        setImageFile(null);
        setFormError(null);
    }

    function updateField<K extends keyof DraftForm>(
        key: K,
        value: DraftForm[K],
    ) {
        setForm((previous) =>
            previous
                ? { ...previous, [key]: value }
                : previous,
        );
    }

    function handleImageChange(
        event: ChangeEvent<HTMLInputElement>,
    ) {
        const file = event.target.files?.[0];

        if (!file) return;

        setFormError(null);

        if (!IMAGE_TYPES.includes(file.type)) {
            setFormError("Chỉ chọn ảnh JPG, PNG hoặc WebP.");
            event.target.value = "";
            return;
        }

        if (file.size > MAX_IMAGE_SIZE) {
            setFormError("Ảnh không được vượt quá 5 MB.");
            event.target.value = "";
            return;
        }

        setImageFile(file);
    }

    async function handleSave(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        if (!form || saving) return;

        setFormError(null);

        const nameVi = form.nameVi.trim();
        const descriptionVi = form.descriptionVi.trim();

        if (!nameVi) {
            setFormError("Vui lòng nhập tên khu bằng tiếng Việt.");
            return;
        }

        if (!descriptionVi) {
            setFormError("Vui lòng nhập mô tả bằng tiếng Việt.");
            return;
        }

        if (!imageFile && !form.currentImageUrl) {
            setFormError("Vui lòng chọn ảnh cho điểm tham quan.");
            return;
        }

        const numberFields: NumberField[] = [
            "latitude",
            "longitude",
            "radiusMeters",
            "mapX",
            "mapY",
            "priority",
        ];

        const hasInvalidNumber = numberFields.some((field) =>
            form[field].trim() === ""
            || !Number.isFinite(Number(form[field])),
        );

        if (hasInvalidNumber) {
            setFormError("Vui lòng nhập đầy đủ các giá trị số.");
            return;
        }

        const latitude = Number(form.latitude);
        const longitude = Number(form.longitude);
        const radiusMeters = Number(form.radiusMeters);
        const mapX = Number(form.mapX);
        const mapY = Number(form.mapY);
        const priority = Number(form.priority);

        if (latitude < -90 || latitude > 90) {
            setFormError("Vĩ độ phải nằm trong khoảng -90 đến 90.");
            return;
        }

        if (longitude < -180 || longitude > 180) {
            setFormError("Kinh độ phải nằm trong khoảng -180 đến 180.");
            return;
        }

        if (radiusMeters <= 0 || radiusMeters > 500) {
            setFormError("Bán kính phải lớn hơn 0 và không quá 500 mét.");
            return;
        }

        if (
            mapX < 0
            || mapX > MAP_WIDTH
            || mapY < 0
            || mapY > MAP_HEIGHT
        ) {
            setFormError("Vị trí trên sơ đồ nằm ngoài kích thước ảnh.");
            return;
        }

        if (
            !Number.isInteger(priority)
            || priority < 0
            || priority > 2147483647
        ) {
            setFormError("Độ ưu tiên phải là số nguyên không âm hợp lệ.");
            return;
        }

        setSaving(true);

        try {
            await savePlaceDraft(form.id, {
                nameVi,
                descriptionVi,
                category: form.category,

                latitude,
                longitude,
                radiusMeters,

                mapX,
                mapY,
                priority,

                image: imageFile,
            });

            setForm(null);
            setImageFile(null);

            // Chỉ thông báo lưu thành công khi API đã trả thành công.
            // Không tự tuyên bố dịch xong.
            setNotice("Đã lưu bản nháp điểm tham quan.");

            await loadPlaces();
        } catch (error) {
            setFormError(
                getErrorMessage(
                    error,
                    "Không lưu được bản nháp. Vui lòng thử lại.",
                ),
            );
        } finally {
            setSaving(false);
        }
    }

    async function handleHide() {
        if (!hideTarget || hiding) return;

        setHiding(true);
        setHideError(null);

        try {
            await placeApi.hide(hideTarget.id);

            setHideTarget(null);
            setNotice("Đã ẩn điểm tham quan.");

            await loadPlaces();
        } catch (error) {
            setHideError(
                getErrorMessage(error, "Không ẩn được điểm tham quan."),
            );
        } finally {
            setHiding(false);
        }
    }

    const displayedImage =
        previewUrl || form?.currentImageUrl || null;

    return (
        <div className= "admin-section" >
        <div className="admin-section__header" >
            <h2>Điểm tham quan({ places.length }) </h2>

                < button
    type = "button"
    className = "admin-btn admin-btn--primary"
    onClick = { openCreate }
        >
        + Thêm điểm tham quan
            </button>
            </div>

    {
        notice && (
            <p role="status" style = {{ color: "#206a45" }
    }>
    { notice }
        </p>
      )
}

{ loading && <p>Đang tải danh sách…</p> }

{
    !loading && listError && (
        <div className="admin-notice admin-notice--warning" >
            <div>
            <strong>Không tải được danh sách </strong>
                < p > { listError } </p>

                < button
    type = "button"
    className = "admin-btn"
    onClick = {() => void loadPlaces()
}
            >
    Thử lại
        </button>
        </div>
        </div>
      )}

{
    !loading && !listError && places.length === 0 && (
        <p>Chưa có điểm tham quan trong cơ sở dữ liệu.</p>
      )
}

{
    !loading && !listError && places.length > 0 && (
        <div className="spots-table" >
            <div className="spots-table__head" >
                <span>Ảnh </span>
                < span > Tên khu </span>
                    < span > Mã khu </span>
                        < span > Loại </span>
                        < span > Trạng thái </span>
                            < span > Thao tác </span>
                                </div>

    {
        places.map((place) => (
            <div className= "spots-table__row" key = { place.id } >
            <span>
            {
                place.imageUrl ? (
                    <img
                    src= { place.imageUrl }
                    alt={ place.name?.vi || place.slug }
                    style={{
            width: 56,
            height: 56,
            objectFit: "cover",
            borderRadius: 8,
        }}
                  />
                ) : (
        <span>Chưa có ảnh </span>
                )
}
</span>

    < span > { place.name?.vi || place.slug } </span>

    < span > { place.slug } </span>

    < span > { CATEGORY_LABELS[place.category]} </span>

    <span>
{ place.isActive ? "Đang bật" : "Đã ẩn" }
</span>

    < span className = "spots-table__actions" >
        <button
                  type="button"
className = "admin-btn admin-btn--sm"
onClick = {() => openEdit(place)}
                >
    Sửa
    </button>

    < button
type = "button"
className = "admin-btn admin-btn--sm admin-btn--danger"
disabled = {!place.isActive}
onClick = {() => {
    setHideError(null);
    setHideTarget(place);
}}
                >
    Ẩn
    </button>
    </span>
    </div>
          ))}
</div>
      )}

{
    form && (
        <div className="admin-modal-overlay" >
            <div
            className="admin-modal admin-modal--wide"
    role = "dialog"
    aria-modal="true"
    aria-labelledby="place-form-title"
        >
        <h3 id="place-form-title" >
        {
            form.id
                ? "Sửa điểm tham quan"
                : "Thêm điểm tham quan"
        }
            </h3>

    {
        form.code ? (
            <p>
            Mã khu: <strong>{ form.code } </strong>
                </p>
            ) : (
            <p>Mã khu sẽ được tạo tự động khi lưu.</p>
            )
    }

    <form onSubmit={ handleSave }>
        <fieldset
                disabled={ saving }
    style = {{
        border: 0,
            padding: 0,
                margin: 0,
                    minWidth: 0,
                }
}
              >
    <div className="admin-form" >
        <div className="admin-form__row" >
            <label className="admin-form__field" >
                <span>Tên khu — tiếng Việt * </span>

                    < input
value = { form.nameVi }
onChange = {(event) =>
updateField("nameVi", event.target.value)
                        }
placeholder = "Ví dụ: Khu voi châu Á"
required
    />
    </label>

    < label className = "admin-form__field" >
        <span>Loại khu * </span>

            < select
value = { form.category }
onChange = {(event) =>
updateField(
    "category",
    event.target.value as Place["category"],
)
                        }
                      >
    {(
        ["animal", "plant", "facility"] as const
    ).map((category) => (
        <option key= { category } value = { category } >
        { CATEGORY_LABELS[category]}
        </option>
    ))}
</select>
    </label>
    </div>

    < label className = "admin-form__field" >
        <span>Mô tả — tiếng Việt * </span>

            < textarea
rows = { 4}
value = { form.descriptionVi }
onChange = {(event) =>
updateField(
    "descriptionVi",
    event.target.value,
)
                      }
placeholder = "Nhập mô tả ngắn về điểm tham quan."
required
    />

    <small>
    Tiếng Việt là nội dung gốc để tạo các bản
                      tiếng Anh, Trung, Hàn và Nhật.
                    </small>
    </label>

    < label className = "admin-form__field" >
        <span>
        { form.id ? "Thay ảnh" : "Ảnh điểm tham quan *" }
        </span>

        < input
type = "file"
accept = "image/jpeg,image/png,image/webp"
onChange = { handleImageChange }
    />

    <small>
    JPG, PNG hoặc WebP, tối đa 5 MB.
                      Khi sửa, không chọn ảnh mới sẽ giữ ảnh cũ.
                    </small>
    </label>

{
    displayedImage && (
        <div>
        <p>Xem trước ảnh </p>

            < img
    src = { displayedImage }
    alt = "Ảnh điểm tham quan đang chọn"
    style = {{
        display: "block",
            width: "100%",
                maxWidth: 420,
                    maxHeight: 260,
                        objectFit: "contain",
                            borderRadius: 10,
                                background: "#f1f5f2",
                        }
}
                      />
    </div>
                  )}

<p className="admin-form__section-label" >
    Vị trí GPS và vùng phát thuyết minh
        </p>

        < div className = "admin-form__row" >
            <label className="admin-form__field" >
                <span>Vĩ độ * </span>

                    < input
type = "number"
min = {- 90}
max = { 90}
step = "any"
value = { form.latitude }
onChange = {(event) =>
updateField("latitude", event.target.value)
                        }
required
    />
    </label>

    < label className = "admin-form__field" >
        <span>Kinh độ * </span>

            < input
type = "number"
min = {- 180}
max = { 180}
step = "any"
value = { form.longitude }
onChange = {(event) =>
updateField("longitude", event.target.value)
                        }
required
    />
    </label>

    < label className = "admin-form__field" >
        <span>Bán kính kích hoạt(mét) * </span>

            < input
type = "number"
min = { 0}
max = { 500}
step = "any"
value = { form.radiusMeters }
onChange = {(event) =>
updateField(
    "radiusMeters",
    event.target.value,
)
                        }
required
    />
    </label>
    </div>

    < p className = "admin-form__section-label" >
        Vị trí hiển thị trên sơ đồ
            </p>

            < div className = "admin-form__row" >
                <label className="admin-form__field" >
                    <span>X trên ảnh(pixel) </span>

                        < input
type = "number"
min = { 0}
max = { MAP_WIDTH }
step = "any"
value = { form.mapX }
onChange = {(event) =>
updateField("mapX", event.target.value)
                        }
required
    />
    </label>

    < label className = "admin-form__field" >
        <span>Y trên ảnh(pixel) </span>

            < input
type = "number"
min = { 0}
max = { MAP_HEIGHT }
step = "any"
value = { form.mapY }
onChange = {(event) =>
updateField("mapY", event.target.value)
                        }
required
    />
    </label>

    < label className = "admin-form__field" >
        <span>Độ ưu tiên </span>

            < input
type = "number"
min = { 0}
max = { 2147483647}
step = { 1}
value = { form.priority }
onChange = {(event) =>
updateField("priority", event.target.value)
                        }
required
    />
    </label>
    </div>

    <small>
                    Vĩ độ, kinh độ dùng để xác định vị trí thực.
    X, Y chỉ đặt marker trên ảnh sơ đồ.
                  </small>

        <p>
                    Bài thuyết minh và âm thanh được quản lý
                    tại mục < strong > Thuyết minh </strong>.
    </p>
    </div>

{
    formError && (
        <p role="alert" style = {{ color: "#b42318" }
}>
{ formError }
    </p>
                )}

<div className="admin-modal__actions" >
    <button
                    type="button"
className = "admin-btn"
onClick = { closeForm }
    >
    Hủy
    </button>

    < button
type = "submit"
className = "admin-btn admin-btn--primary"
    >
{ saving? "Đang lưu…": "Lưu bản nháp" }
    </button>
    </div>
    </fieldset>
    </form>
    </div>
    </div>
      )}

{
    hideTarget && (
        <div className="admin-modal-overlay" >
            <div
            className="admin-modal"
    role = "dialog"
    aria-modal="true"
    aria-labelledby="hide-place-title"
        >
        <h3 id="hide-place-title" > Ẩn điểm tham quan </h3>

            <p>
              Bạn muốn ẩn{ " " }
    <strong>
        { hideTarget.name?.vi || hideTarget.slug }
        </strong>
        ?
        </p>

        <p>
              Dữ liệu vẫn được giữ trong hệ thống.
            </p>

    {
        hideError && (
            <p role="alert" style = {{ color: "#b42318" }
    }>
    { hideError }
        </p>
            )
}

<div className="admin-modal__actions" >
    <button
                type="button"
className = "admin-btn"
disabled = { hiding }
onClick = {() => setHideTarget(null)}
              >
    Hủy
    </button>

    < button
type = "button"
className = "admin-btn admin-btn--danger"
disabled = { hiding }
onClick = {() => void handleHide()}
              >
{ hiding? "Đang ẩn…": "Ẩn khu" }
    </button>
    </div>
    </div>
    </div>
      )}
</div>
  );
}