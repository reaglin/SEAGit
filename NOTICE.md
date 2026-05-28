# Third-Party Notices

SEAGit bundles the following third-party software. Their licenses apply to
those components only; SEAGit itself is distributed under its own license.

## Git for Windows / MinGit

SEAGit ships **MinGit** (a redistributable subset of Git for Windows) inside
the application package so users do not need to install Git separately. MinGit
is unmodified and lives entirely under `SEAGit\MinGit\` in the install
folder; SEAGit invokes it as an external process via
`MinGit\cmd\git.exe`.

- Project:  Git for Windows — https://gitforwindows.org/
- Source:   https://github.com/git-for-windows/git
- License:  GNU General Public License, version 2.0
            (full text: `SEAGit\MinGit\LICENSE.txt`)

The components inside MinGit (curl, OpenSSL, zlib, libssh2, gettext, libiconv,
libssl, libffi, ncurses, openssl, pcre2, zstd, nghttp2, libpsl, libsqlite,
libtasn1, libunistring, libwinpthread, expat, brotli, gcc-libs, etc.) are
distributed under their respective licenses; the full text of each is included
under `SEAGit\MinGit\mingw64\share\licenses\` and
`SEAGit\MinGit\usr\share\licenses\` in the install.

Per GPLv2 §3, the complete corresponding source code for Git is available from
the upstream project at https://github.com/git-for-windows/git for at least
three years from the date this version of SEAGit was distributed. No charge
will be made for the source by SEAGit beyond the cost of physically performing
source distribution, should a written request be received.
