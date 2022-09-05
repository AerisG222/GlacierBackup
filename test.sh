#! /bin/bash
cd src/GlacierBackup

dotnet run \
    glacier_backup \
    us-east-1 \
    test \
    full \
    /srv/www/website_assets/images/2022/eighth_grade/src \
    /srv/www/website_assets/images/ \
    photosql \
    /home/mmorano/Desktop/glaciertest.sql \
    ~/.aws/credentials

cd ../..
