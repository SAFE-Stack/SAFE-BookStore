// File adapted from https://github.com/fable-compiler/webpack-config-template
var path = require("path");

function resolve(filePath) {
    return path.join(__dirname, filePath)
}

var CONFIG = {
    fsharpEntry:
        ["whatwg-fetch",
            "@babel/polyfill",
            resolve("./Client.fsproj")
        ],
    outputDir: resolve("./public"),
    devServerPort: undefined,
    devServerProxy: {
        '/api/*': {
            target: 'http://localhost:' + (process.env.SUAVE_FABLE_PORT || "8085"),
            changeOrigin: true
        }
    },
    historyApiFallback: {
        index: resolve("./index.html")
    },
    contentBase: __dirname,
    // Use babel-preset-env to generate JS compatible with most-used browsers.
    // More info at https://github.com/babel/babel/blob/master/packages/babel-preset-env/README.md
    babel: {
        presets: [
            ["@babel/preset-env", {
                "targets": {
                    "browsers": ["last 2 versions"]
                },
                "modules": false,
                "useBuiltIns": "usage",
            }]
        ],
        plugins: ["@babel/plugin-transform-runtime"]
    }
}

// If we're running the webpack-dev-server, assume we're in development mode
var isProduction = !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1);
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

var path = require("path");
var webpack = require("webpack");
var UglifyJSPlugin = require('uglifyjs-webpack-plugin');

var commonPlugins = [
];

module.exports = {
    entry: CONFIG.fsharpEntry,
    // NOTE we add a hash to the output file name in production
    // to prevent browser caching if code changes
    output: {
        path: CONFIG.outputDir,
        publicPath: "/public",
        filename: '[name].js'
    },
    resolve: {
        symlinks: false,
    },
    mode: isProduction ? "production" : "development",
    devtool: isProduction ? undefined : "source-map",
    optimization: {
        // Split the code coming from npm packages into a different file.
        // 3rd party dependencies change less often, let the browser cache them.
        splitChunks: {
            cacheGroups: {
                commons: {
                    test: /node_modules/,
                    name: "vendors",
                    chunks: "all"
                }
            }
        },
    },
    // Besides the HtmlPlugin, we use the following plugins:
    // PRODUCTION
    //      - UglifyJSPlugin: Minimize the CSS
    // DEVELOPMENT
    //      - HotModuleReplacementPlugin: Enables hot reloading when code changes without refreshing
    plugins: isProduction ?
        commonPlugins.concat([
            // Inlining is causing problems in minified code
            // See https://github.com/mishoo/UglifyJS2/issues/2842#issuecomment-359527962
            new UglifyJSPlugin({
                uglifyOptions: {
                    compress: { inline: false }
                }
            }),
        ])
        : commonPlugins.concat([
            new webpack.HotModuleReplacementPlugin(),
        ]),
    // Configuration for webpack-dev-server
    devServer: {
        proxy: CONFIG.devServerProxy,
        hot: true,
        inline: true,
        historyApiFallback: CONFIG.historyApiFallback,
        contentBase: CONFIG.contentBase
    },
    // - fable-loader: transforms F# into JS
    // - babel-loader: transforms JS to old syntax (compatible with old browsers)
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: "fable-loader"
            },
            {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: CONFIG.babel
                },
            }
        ]
    }
};
