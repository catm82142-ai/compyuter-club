import { app } from "./app";

const port = Number(process.env.PORT ?? 4000);

app.listen(port, () => {
  console.log(`Computer Club MVP server running on http://localhost:${port}`);
});
