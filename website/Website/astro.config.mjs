// @ts-check
import { defineConfig } from 'astro/config';
import vue from '@astrojs/vue';
import tailwind from '@astrojs/tailwind';

// https://astro.build/config
export default defineConfig({
  output: 'static',
  outDir: './dist',
  publicDir: './public',
  // .NET Aspire integration settings
  base: '/',
  trailingSlash: 'ignore',
  integrations: [
    vue({
      reactivityTransform: true,
      devtools: true,
    }),
    tailwind({
      applyBaseStyles: false, // We'll handle base styles ourselves for shadcn/ui
      config: {
        path: './tailwind.config.mjs',
      },
    }),
  ],
  build: {
    inlineStylesheets: 'never', // Keep CSS external for better caching
    assets: '_astro',
    assetsPrefix: '/_astro/', // Explicit assets prefix for CDN
    // split: true, // Removed - not a valid option in newer Astro versions
  },
  compressHTML: true,
  prefetch: {
    prefetchAll: false,
    defaultStrategy: 'hover',
  },
  vite: {
    resolve: {
      alias: {
        '@': new URL('./src', import.meta.url).pathname,
      },
    },
    build: {
      cssCodeSplit: true,
      minify: 'esbuild',
      target: 'es2020', // Modern target for better optimization
      rollupOptions: {
        output: {
          // Enhanced chunking strategy for optimal CDN caching
          manualChunks: {
            'vue-vendor': ['vue', '@vueuse/core'],
            'ui-vendor': ['lucide-vue-next', 'radix-vue'],
            utils: ['clsx', 'tailwind-merge'],
          },
          // Generate consistent hashed filenames for immutable caching
          entryFileNames: 'assets/[name].[hash].js',
          chunkFileNames: 'assets/[name].[hash].js',
          assetFileNames: assetInfo => {
            // Use newer API for file extensions
            const ext =
              assetInfo.type === 'asset' && assetInfo.source
                ? assetInfo.fileName
                  ? assetInfo.fileName.split('.').pop()
                  : null
                : null;

            if (ext && /^(png|jpe?g|svg|gif|tiff|bmp|ico)$/i.test(ext)) {
              return `assets/images/[name].[hash].${ext}`;
            } else if (ext && /^(woff|woff2|eot|ttf|otf)$/i.test(ext)) {
              return `assets/fonts/[name].[hash].${ext}`;
            } else if (ext && /^css$/i.test(ext)) {
              return `assets/styles/[name].[hash].${ext}`;
            }
            return `assets/[name].[hash].[ext]`;
          },
        },
      },
      // Optimize for CDN compression
      reportCompressedSize: true,
      chunkSizeWarningLimit: 1000, // 1MB warning limit
    },
    server: {
      host: '0.0.0.0', // Expose to local network and .NET hosting
      port: 4321, // Astro dev server port
      strictPort: true, // Fail if port is already in use
      cors: true, // Enable CORS for .NET proxy integration
      // Proxy configuration for development integration with .NET hosting
      proxy: {
        // Forward API calls to Public Gateway through .NET hosting
        '/api': {
          target: process.env.VITE_PUBLIC_GATEWAY_URL || 'http://localhost:7220',
          changeOrigin: true,
          secure: false,
          ws: true,
        },
        // Health check proxy for Aspire monitoring
        '/health': {
          target: 'http://localhost:5000', // .NET Website host
          changeOrigin: true,
          secure: false,
        },
      },
    },
    // Environment variable handling for .NET Aspire integration
    define: {
      __ASPIRE_ENVIRONMENT__: JSON.stringify(process.env.ASPIRE_ENVIRONMENT || 'Development'),
      __PUBLIC_GATEWAY_URL__: JSON.stringify(process.env.VITE_PUBLIC_GATEWAY_URL || 'http://localhost:7220'),
    },
    // Optimize dependencies for better CDN caching
    optimizeDeps: {
      include: ['vue', '@vueuse/core', 'lucide-vue-next', 'radix-vue'],
      exclude: [],
    },
    // CSS configuration for Tailwind and shadcn-vue integration
    css: {
      postcss: {
        plugins: [],
      },
    },
  },
  // CDN-optimized image settings
  image: {
    service: {
      entrypoint: 'astro/assets/services/sharp',
      config: {
        limitInputPixels: false,
      },
    },
  },
  // Remove experimental features for now
  // experimental: {},
});
