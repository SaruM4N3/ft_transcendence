import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join } from 'node:path';

const PUBLIC_DIR = join(import.meta.dirname, 'public');
const PORT = 80;

const MIME_TYPES = {
	'.html': 'text/html',
	'.css': 'text/css',
	'.js': 'text/javascript'
};

const server = createServer(async (req, res) => {
	let urlPath = req.url;
	if (urlPath === '/') {
		urlPath = '/index.html';
	}
	const filePath = join(PUBLIC_DIR, urlPath);

	try {
		const content = await readFile(filePath);
		let contentType = MIME_TYPES[extname(filePath)];
		if (!contentType) {
			contentType = 'application/octet-stream';
		}
		res.writeHead(200, { 'Content-Type': contentType });
		res.end(content);
	} catch {
		res.writeHead(404, { 'Content-Type': 'text/plain' });
		res.end('Not found');
	}
});

server.listen(PORT, () => {
	console.log(`Listening on port ${PORT}`);
});
