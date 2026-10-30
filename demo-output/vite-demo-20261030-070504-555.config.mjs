import originalConfig from '../web/vite.config.ts';

export default async function (environment) {
    const config = await (typeof originalConfig === 'function'
        ? originalConfig(environment)
        : originalConfig);
    return {
        ...config,
        server: {
            ...config.server,
            host: '127.0.0.1',
            port: 5173,
            strictPort: true,
            allowedHosts: ["boundary-remained-movie-votes.trycloudflare.com"],
            proxy: {
                ...config.server?.proxy,
                '/api': { target: 'http://localhost:5230', changeOrigin: true },
                '/storage': { target: 'http://localhost:5230', changeOrigin: true },
            },
        },
    };
}
