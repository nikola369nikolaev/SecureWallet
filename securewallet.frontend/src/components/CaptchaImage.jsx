export function CaptchaImage({ imageBase64 }) {
  if (!imageBase64) {
    return null;
  }

  return (
    <div className="captcha-block">
      <p className="field-hint">Разчети символите от картинката и ги въведи в полето по-долу.</p>
      <img
        className="captcha-image"
        src={`data:image/svg+xml;base64,${imageBase64}`}
        alt="Captcha изображение"
      />
    </div>
  );
}