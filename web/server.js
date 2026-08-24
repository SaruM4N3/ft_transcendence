import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { extname, join } from 'node:path';

const PUBLIC_DIR = join(import.meta.dirname, 'public');
const PORT = 80;

const MIME_TYPES = {
	'.html': 'text/html',
	'.css': 'text/css',
	'.js': 'text/javascript',
	'.json': 'application/json',
	'.wasm': 'application/wasm',
	'.ico': 'image/x-icon',
	'.png': 'image/png',
	'.data': 'application/octet-stream'
};

function contentTypeFor(filePath) {
	return MIME_TYPES[extname(filePath)] || 'application/octet-stream';
}

// Unity WebGL build files (.data.gz/.framework.js.gz/.wasm.gz) are served
// pre-compressed; the browser needs Content-Encoding: gzip to unpack them,
// with the Content-Type of the *decompressed* file underneath.
async function resolveFile(urlPath) {
	let filePath = join(PUBLIC_DIR, urlPath);
	try {
		const stats = await stat(filePath);
		if (stats.isDirectory()) {
			filePath = join(filePath, 'index.html');
		}
	} catch {
		// let readFile surface the real error below
	}
	return filePath;
}

const server = createServer(async (req, res) => {
	const urlPath = req.url.split('?')[0];
	const filePath = await resolveFile(urlPath === '/' ? '/index.html' : urlPath);

	try {
		const content = await readFile(filePath);
		const headers = {};

		if (filePath.endsWith('.gz')) {
			headers['Content-Encoding'] = 'gzip';
			headers['Content-Type'] = contentTypeFor(filePath.slice(0, -3));
		} else {
			headers['Content-Type'] = contentTypeFor(filePath);
		}

		res.writeHead(200, headers);
		res.end(content);
	} catch {
		res.writeHead(404, { 'Content-Type': 'text/plain' });
		res.end('Not found');
	}
});

server.listen(PORT, () => {
	console.log(`Listening on port ${PORT}`);
});
