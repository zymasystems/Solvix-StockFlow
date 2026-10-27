# Home page styles

The Home/Index.cshtml design was supplied in code.html on 2026-10-27. Its Tailwind theme is retained here and the generated CSS is committed, so production does not need the Tailwind browser CDN or Node.js.

From the repository root, regenerate after editing the home page:

```sh
npx --yes tailwindcss@3.4.17 --config tools/homepage/tailwind.config.cjs --input tools/homepage/input.css --output web/wwwroot/css/homepage.css --minify
```

Dashboard controls and telemetry on this public page are illustrative. Account buttons use the real MVC account routes. The video is the sample clip provided with the uploaded design; replace its source with an approved warehouse video before using it as a real facility feed.
